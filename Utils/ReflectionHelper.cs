using System;
using System.Reflection;
using MelonLoader;

namespace HBMP.Utils
{
    public class ReflectionHelper
    {
        public static T GetPrivateField<T>(object fieldHolder, string fieldName)
        {
            if (fieldHolder == null)
            {
                return default;
            }

            Type type = fieldHolder.GetType();
            FieldInfo fieldInfo =
                type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                MelonLogger.Error("Attempted to get a private field which does not exist.");
                return default;
            }

            return (T)fieldInfo.GetValue(fieldHolder);
        }
    }
}