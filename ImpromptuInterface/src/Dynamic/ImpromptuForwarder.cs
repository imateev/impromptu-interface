﻿// 
//  Copyright 2011 Ekon Benefits
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using ImpromptuInterface.Optimization;
namespace ImpromptuInterface.Dynamic
{
    ///<summary>
    /// Proxies Calls allows subclasser to override do extra actions before or after base invocation
    ///</summary>
    /// <remarks>
    /// This may not be as efficient as other proxies that can work on just static objects or just dynamic objects...
    /// Consider this when using.
    /// </remarks>
    public abstract class ImpromptuForwarder:ImpromptuObject
    {
        protected ImpromptuForwarder(object target)
        {
            Target = target;
        }

#if !SILVERLIGHT
        protected ImpromptuForwarder(SerializationInfo info, 
           StreamingContext context):base(info,context)
        {


            Target = info.GetValue<IDictionary<string, object>>("Target");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info,context);
            info.AddValue("Target", Target);
        }
#endif

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (!KnownInterfaces.Any())
            {
                if (Target is DynamicObject)
                {
                    return ((DynamicObject) Target).GetDynamicMemberNames();
                }
                if (!(Target is IDynamicMetaObjectProvider))
                    return Target.GetType().GetMembers(BindingFlags.Public).Select(it => it.Name).ToList();
            }
            return base.GetDynamicMemberNames();
        }


        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        public object Target {  get; protected set; }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (Target == null)
            {
                result = null;
                return false;
            }

            if (Impromptu.InvokeIsEvent(Target, binder.Name))
            {
                result = new ImpromptuForwarderAddRemove();
                return true;
            }

            result = Impromptu.InvokeGet(Target, binder.Name);

            return true;

        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (Target == null)
            {
                result = null;
                return false;
            }

            object[] tArgs = Util.NameArgsIfNecessary(binder.CallInfo, args);

            try
            {
                result = Impromptu.InvokeMember(Target, binder.Name, tArgs);
               
            }
            catch (RuntimeBinderException)
            {
                result = null;
                try
                {
                    Impromptu.InvokeMemberAction(Target, binder.Name, tArgs);
                }
                catch (RuntimeBinderException)
                {

                    return false;
                }
            }
            return true;
        }

       

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (Target == null)
            {
                return false;
            }

            if (Impromptu.InvokeIsEvent(Target, binder.Name) && value is ImpromptuForwarderAddRemove)
            {
                var tValue = value as ImpromptuForwarderAddRemove;

                if (tValue.IsAdding)
                {
                    Impromptu.InvokeAddAssign(Target,binder.Name, tValue.Delegate);
                }
                else
                {
                    Impromptu.InvokeSubtractAssign(Target, binder.Name, tValue.Delegate);
                }

                return true;
            }

            Impromptu.InvokeSet(Target, binder.Name, value);

            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (Target == null)
            {
                result = null;
                return false;
            }

            object[] tArgs = Util.NameArgsIfNecessary(binder.CallInfo, indexes);

            result = Impromptu.InvokeGetIndex(Target, tArgs);
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (Target == null)
            {
                return false;
            }

            var tCombinedArgs = indexes.Concat(new[] { value }).ToArray();
            object[] tArgs = Util.NameArgsIfNecessary(binder.CallInfo, tCombinedArgs);

            Impromptu.InvokeSetIndex(tArgs);
            return true;
        }


        /// <summary>
        /// Equals the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(ImpromptuForwarder other)
        {
            if (ReferenceEquals(null, other)) return ReferenceEquals(null, Target);
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Target, Target);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return ReferenceEquals(null, Target); 
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ImpromptuForwarder)) return false;
            return Equals((ImpromptuForwarder) obj);
        }

        public override int GetHashCode()
        {
            return (Target != null ? Target.GetHashCode() : 0);
        }
    }
}
