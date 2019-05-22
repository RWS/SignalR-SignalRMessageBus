using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;
using Owin;

namespace Sdl.SignalR.Backplane.Common
{
    public class HubLoader
    {
        /// <summary>
        /// Instantiates message bus and maps SignalR hubs to the app builder.
        /// </summary>
        /// <param name="app">An instance of an <see cref="IAppBuilder"/> used in IIS hosted application.</param>
        /// <param name="assembly">Full assembly name that contains message bus and scaleout configuration implementation.</param>
        /// <param name="scaleoutConfigurationType">Type name of scaleout configuration used to configure message bus. This type must be inherited from <see cref="ScaleoutConfiguration"/></param>
        /// <param name="scaleoutMessageBusType">Type name of a message bus. This type must implement <see cref="IMessageBus"/> interface.</param>
        /// <param name="hubAssemblyName">Full assembly name that contains hub interface.</param>
        /// <param name="patchOnReceived">Specify <c>true</c> to include PayloadId of a message inside the message body.</param>
        /// <param name="backplaneConfigurationParameters">Parameters we need to create a backplane instance (e.g. backplane connectionString)</param>
        public static void Load(IAppBuilder app, string assembly, string scaleoutConfigurationType,
            string scaleoutMessageBusType, string hubAssemblyName, bool patchOnReceived, params object[] backplaneConfigurationParameters)
        {
            IMessageBus messageBus = CreateMessageBus(assembly, scaleoutConfigurationType, scaleoutMessageBusType, patchOnReceived, backplaneConfigurationParameters);

            Trace.WriteLine("Starting hub host.");

            GlobalHost.DependencyResolver.Register(typeof(IMessageBus), () => messageBus);

            // Add tracing for errors that occur inside hub message handlers
            GlobalHost.HubPipeline.AddModule(new ErrorHandlingPipelineModule());
            HubConfiguration hubConfiguration = new HubConfiguration { EnableDetailedErrors = true };

            // Load notification hub implementation into current assembly
            Trace.WriteLine(string.Format("Loading '{0}' hub implementation.", hubAssemblyName));
            AppDomain.CurrentDomain.Load(hubAssemblyName);

            app.MapSignalR(hubConfiguration);

            Trace.WriteLine(string.Format("Starting hub host '{0}'.", scaleoutMessageBusType));
        }

        /// <summary>
        /// Instantiates message bus.
        /// </summary>
        /// <param name="assembly">Full assembly name that contains message bus and scaleout configuration implementation.</param>
        /// <param name="scaleoutConfigurationType">Type name of scaleout configuration used to configure message bus. This type must be inherited from <see cref="ScaleoutConfiguration"/></param>
        /// <param name="scaleoutMessageBusType">Type name of a message bus. This type must implement <see cref="IMessageBus"/> interface.</param>
        /// <param name="backplaneConfigurationParameters"></param>
        /// <returns>Message bus <see cref="IMessageBus"/> instanse.</returns>
        public static IMessageBus CreateMessageBus(string assembly, string scaleoutConfigurationType, string scaleoutMessageBusType,
            params object[] backplaneConfigurationParameters)
        {
            return CreateMessageBus(assembly, scaleoutConfigurationType, scaleoutMessageBusType, true,
                backplaneConfigurationParameters);
        }

        public static IMessageBus CreateMessageBus(string assembly, string scaleoutConfigurationType, string scaleoutMessageBusType,
            bool patchOnReceived, params object[] backplaneConfigurationParameters)
        {
            Assembly backplaneAssembly = Assembly.Load(assembly);

            Type messageBusType = backplaneAssembly.GetType(scaleoutMessageBusType);

            if (!typeof(IMessageBus).IsAssignableFrom(messageBusType))
            {
                throw new ConfigurationErrorsException(
                    string.Format("Provided ScaleoutMessageBusType '{0}' must implement IMessageBus interface.",
                        scaleoutMessageBusType));
            }
            Type backplaneConfigType = backplaneAssembly.GetType(scaleoutConfigurationType);
            if (!typeof(ScaleoutConfiguration).IsAssignableFrom(backplaneConfigType))
            {
                throw new ConfigurationErrorsException(
                    string.Format("Provided config type '{0}' must be inherited from ScaleoutConfiguration type.",
                        scaleoutConfigurationType));
            }

            IMessageBus messageBus;

            try
            {
                if (patchOnReceived)
                {
                    messageBus = LoadBackplane(messageBusType, backplaneConfigType, backplaneConfigurationParameters);
                }
                else
                {
                    object backplaneConfig = Activator.CreateInstance(backplaneConfigType, backplaneConfigurationParameters);
                    TryToSetStreamIndex("TopicCount", backplaneConfig, backplaneConfigType);

                    messageBus = (IMessageBus)Activator.CreateInstance(messageBusType, GlobalHost.DependencyResolver, backplaneConfig);
                }
            }
            catch (MissingMethodException e)
            {
                throw new Exception("Unable to initialize SignalR Backplane Hub. Please provide correct configuration parameters according to the configured backplane type.", e);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to connect to SignalR Backplane Hub. Please check the configured backplane connection string and its parameters.", e);
            }

            return messageBus;
        }

        /// <summary>
        /// This piece of code we need for loading azure message bus backplane.
        /// Currently our architecture does not support multiple topics(tables,queues, etc).
        /// By default there is only one table for Sql & Oracle backplanes, the default
        /// setting for azure service bus is 5. With this dirty hack we set it to 1. Shame on me.
        /// 
        /// There was also an idea to send mappingId & streamIndex with the message and create
        /// several virtual streams for sdl.signalR backplane, but this streamCount property in CM
        /// should be syncronized with the streamCount property in CME. And we don't have
        /// time to do it right now.
        /// </summary>
        private static void TryToSetStreamIndex(string propertyName, object configuration, Type configurationType)
        {
            try
            {
                PropertyInfo property = configurationType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    property.SetValue(configuration, 1);
                }
            }
            catch { }
        }

        /// <summary>
        /// Dynamically overrides backplane behavior in order to support Sdl backplane strategy and
        /// creates configured instance which implements <see cref="IMessageBus"/>.
        /// </summary>
        /// <param name="backplaneType">Backplane type (must inherit <see cref="ScaleoutMessageBus"/>). </param>
        /// <param name="configurationType">Backplane configuration type (must inherit <see cref="ScaleoutConfiguration"/></param>
        /// <param name="configurationParams">Backplane configuration parameters.</param>
        /// <returns>Instance </returns>
        private static IMessageBus LoadBackplane(Type backplaneType, Type configurationType,
            params object[] configurationParams)
        {
            string methodName = "OnReceived";
            MethodInfo methodInfo = typeof (HubLoader).GetMethod(methodName);
            Type patchedType = ConstructTypeWithOverride(
                "SdlMessageBusAssembly",
                "SdlMessageBus",
                methodName,
                methodInfo,
                backplaneType);

            object backplaneConfig = (ScaleoutConfiguration)Activator.CreateInstance(configurationType, configurationParams);

            TryToSetStreamIndex("TopicCount", backplaneConfig, configurationType);

            IMessageBus messageBus = (IMessageBus)Activator.CreateInstance(patchedType, GlobalHost.DependencyResolver, backplaneConfig);

            return messageBus;
        }

        /// <summary>
        /// Generates new type with method override.
        /// </summary>
        /// <param name="assemblyPrefix">Prefix for generated assembly.</param>
        /// <param name="subClassPrefix">Prefix for generated type name.</param>
        /// <param name="methodName">The name of the method to be overrided. </param>
        /// <param name="overridedMethodInfo">Method contains new logic for overrided method.</param>
        /// <param name="baseType">The type of the new method.</param>
        /// <returns></returns>
        private static Type ConstructTypeWithOverride(
            string assemblyPrefix,
            string subClassPrefix,
            string methodName,
            MethodInfo overridedMethodInfo,
            Type baseType)
        {
            AssemblyName dynamicAssemblyName = new AssemblyName(GetUniqueName(assemblyPrefix));
            AssemblyBuilder dynamicAssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(dynamicAssemblyName,
                AssemblyBuilderAccess.Run);
            
            ModuleBuilder mod = dynamicAssemblyBuilder.DefineDynamicModule(GetUniqueName(assemblyPrefix + "_module"));
            TypeBuilder derivedType = mod.DefineType(GetUniqueName(string.Format("{0}_{1}", subClassPrefix, baseType.Name)),
                baseType.Attributes, baseType);
            
            MethodInfo baseMethod = baseType.GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            Type[] parentMethodParameterTypes = baseMethod.GetParameters().Select(c => c.ParameterType).ToArray();
            MethodBuilder methodBuilder = derivedType.DefineMethod(methodName,
                MethodAttributes.Family | MethodAttributes.Virtual, null, parentMethodParameterTypes);

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            for (int i = 0; i <= parentMethodParameterTypes.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }

            ilGenerator.EmitCall(OpCodes.Call, overridedMethodInfo, null);

            for (int i = 0; i <= parentMethodParameterTypes.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }

            ilGenerator.EmitCall(OpCodes.Call, baseMethod, null);
            ilGenerator.Emit(OpCodes.Ret);

            derivedType.DefineMethodOverride(
                 methodBuilder,
                 baseType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy));

            ConstructorInfo baseCtor = baseType.GetConstructors(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)[0];

            Type[] constructorParameterTypes = baseCtor.GetParameters().Select(c => c.ParameterType).ToArray();
            ConstructorBuilder constructorBuilder = derivedType.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.HasThis,
                constructorParameterTypes);

            ILGenerator constructorBuilderGenerator = constructorBuilder.GetILGenerator();
            for (int i = 0; i <= constructorParameterTypes.Length; i++)
            {
                constructorBuilderGenerator.Emit(OpCodes.Ldarg, i);
            }

            constructorBuilderGenerator.Emit(OpCodes.Call, baseCtor);
            constructorBuilderGenerator.Emit(OpCodes.Ret);

            Type newType = derivedType.CreateType();
            return newType;
        }

        private static string GetUniqueName(string prefix)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                prefix = prefix.Replace(' ', ' ');
                sb.Append(prefix).Append("_");
            }

            sb.Append(Guid.NewGuid().ToString("N").Substring(0, 8));
            return sb.ToString();
        }

        public void OnReceived(int streamIndex, ulong id, ScaleoutMessage message)
        {
            JsonSerializer serializer = new JsonSerializer();

            foreach (Message msg in message.Messages)
            {
                string msgString;
                using (ArraySegmentTextReader astr = new ArraySegmentTextReader(msg.Value, msg.Encoding))
                {
                    msgString = astr.ReadToEnd();
                }

                ClientHubInvocation clientHubInvocation = serializer.Parse<ClientHubInvocation>(msgString);

                if (clientHubInvocation.Args != null && clientHubInvocation.Args.Length == 1)
                {
                    byte[] orginalArr = Convert.FromBase64String(clientHubInvocation.Args[0].ToString());
                    var idArr = BitConverter.GetBytes(id);
                    byte[] resultArr = new byte[orginalArr.Length + idArr.Length];

                    idArr.CopyTo(resultArr, 0);
                    orginalArr.CopyTo(resultArr, idArr.Length);

                    clientHubInvocation.Args[0] = Convert.ToBase64String(resultArr.ToArray());

                    StringBuilder sb = new StringBuilder();
                    using (StringWriter sr = new StringWriter(sb))
                    {
                        serializer.Serialize(sr, clientHubInvocation);
                        sr.Flush();
                    }

                    msg.Value = new ArraySegment<byte>(msg.Encoding.GetBytes(sb.ToString()));
                }
            }
        }
    }
}
