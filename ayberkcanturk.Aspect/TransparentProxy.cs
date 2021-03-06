﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace ayberkcanturk.Aspect
{
    using Core;

    [DebuggerStepThrough]
    public class TransparentProxy<TI, T> : RealProxy where T : TI, new()
    {
        private readonly TI instance;

        public TransparentProxy() : base(typeof(TI))
        {
            this.instance = new T();
        }

        internal TransparentProxy(TI instance) : base(typeof(TI))
        {
            this.instance = instance;
        }

        public override IMessage Invoke(IMessage message)
        {
            IMethodCallMessage methodCallMessage = message as IMethodCallMessage;
            Type realType = typeof(T);
            MethodInfo methodInfo = realType.GetMethod(methodCallMessage.MethodName);

            try
            {
                object[] interceptors = methodInfo.GetCustomAttributes(typeof(IInterceptor), true);
                IInvocation invocation = null;
                object response = null;

                if (interceptors.Length > 0)
                {
                    invocation = new Invocation<TI>(instance, methodCallMessage, methodInfo.ReturnType);
                    foreach (IInterceptor interceptor in interceptors)
                    {
                        interceptor.Intercept(ref invocation);
                        if (invocation.Response != null)
                        {
                            response = invocation.Response;
                            //Return as soon as possible.
                            return new ReturnMessage(response, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
                        }
                    }
                }

                if (invocation != null && invocation.IsProcceeded.Equals(false))
                {
                    response = methodCallMessage.MethodBase.Invoke(instance, methodCallMessage.InArgs);
                }

                return new ReturnMessage(response, null, 0, methodCallMessage.LogicalCallContext, methodCallMessage);
            }
            catch (Exception e)
            {
                return new ReturnMessage(e, methodCallMessage);
            }
        }
    }
}
