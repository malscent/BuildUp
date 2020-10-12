using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildUp.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsDelete(this Type eventType)
        {
            return eventType.CustomAttributes.Any(t => t.AttributeType.FullName == "BuildUp.DeleteAttribute");
        }

        public static bool IsPatch(this Type eventType)
        {
            return eventType.CustomAttributes.Any(t => t.AttributeType.FullName == "BuildUp.PatchAttribute");
        }

        public static Type[] GetCreateTypes(this Type eventType)
        {

            var attr = eventType.GetCustomAttributes(typeof(CreatesAttribute), true);
            if (attr.Length == 0)
            {
                return new Type[0];
            }
            var newTypes = new List<Type>();
            foreach (var t in attr)
            {
                if (t is CreatesAttribute create)
                {
                    newTypes.Add(create.Creates);
                }
            }
            return newTypes.ToArray();
        }
    }
}