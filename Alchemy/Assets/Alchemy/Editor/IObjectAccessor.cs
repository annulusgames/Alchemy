using UnityEditor;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Alchemy.Editor
{
    public interface IObjectAccessor
    {
        public object Target { get; set; }
        static  Regex indexRegex = new Regex(@"[^0-9]");
        public static IObjectAccessor Create(SerializedProperty property)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            var splits = property.propertyPath.Split('.');
            
            IObjectAccessor accessor=new IdentityAccessor(targetObject);

            for (var index = 0; index < splits.Length; index++)
            {
                var t = splits[index];
                if (t == "Array")
                {
                    var listIndex = int.Parse(indexRegex.Replace(splits[index + 1], ""));
                    accessor = new IndexerAccessor(accessor, listIndex);
                    index++;
                    continue;
                }

                accessor = accessor.Create(ReflectionHelper.GetField(accessor.Target.GetType(), t, includingBaseNonPublic: true));
            }

            return accessor;
        }
        public IObjectAccessor Create(FieldInfo fieldInfo)
        {
            if(fieldInfo.IsStatic)return new DelegateAccessor(()=>fieldInfo.GetValue(null),x=>fieldInfo.SetValue(null,x));
            return new ReflectionFieldAccessor(this, fieldInfo);
        }
        
        public IObjectAccessor Create(PropertyInfo propertyInfo)
        {
            if(propertyInfo.GetMethod.IsStatic)return new DelegateAccessor(()=>propertyInfo.GetValue(null),null);
            return new ReflectionPropertyAccessor(this, propertyInfo);
        }
        
    }
    
    
    public class IdentityAccessor : IObjectAccessor
    {
        public IdentityAccessor(object target)
        {
            Target = target;
        }
        
        public object Target { get; set; }
    }
    public class IndexerAccessor:IObjectAccessor
    {
        public IndexerAccessor(IObjectAccessor parent, int index)
        {
            Parent = parent;
            Index = index;
        }
        
        public IObjectAccessor Parent { get;  }
        
        public int Index { get;  }

        public object Target
        {
            get
            {
                var list =(System.Collections.IList) Parent.Target;
            
                return list[Index];
            }
            set
            {
                var list =(System.Collections.IList) Parent.Target;
                list[Index] = value;
            }
        }
    }
    
    public class ReflectionFieldAccessor : IObjectAccessor
    {
        public ReflectionFieldAccessor(IObjectAccessor parent,FieldInfo fieldInfo)
        {
            Parent = parent;
           FieldInfo = fieldInfo;
        }
        
        public IObjectAccessor Parent { get;  }
        
        public FieldInfo FieldInfo { get;  }

        public object Target
        {
            get { return FieldInfo.GetValue(Parent.Target); }
            set
            {
                var parentTarget = Parent.Target;
                FieldInfo.SetValue(parentTarget, value);
                Parent.Target = parentTarget;
            }
        }
    }
    public class ReflectionPropertyAccessor : IObjectAccessor
    {
        public ReflectionPropertyAccessor(IObjectAccessor parent,PropertyInfo propertyInfo)
        {
            Parent = parent;
            this.propertyInfo = propertyInfo;
        }
        
        public IObjectAccessor Parent { get;  }
        
        PropertyInfo propertyInfo { get;  }

        public object Target
        {
            get { return propertyInfo.GetValue(Parent.Target); }
            set
            {
                if(propertyInfo.CanWrite==false)return;
                var parentTarget = Parent.Target;
                propertyInfo.SetValue(parentTarget, value);
                Parent.Target = parentTarget;
            }
        }
    }

    public class DelegateAccessor : IObjectAccessor
    {
        public DelegateAccessor(Func<object> getter,Action<object> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }
        public object Target
        {
            get => getter?.Invoke();
            set => setter?.Invoke(value);
        }

        readonly Func<object> getter;
        readonly Action<object> setter;
    }
}