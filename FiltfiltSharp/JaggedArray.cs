using System;
using System.Collections.Generic;

public class JaggedArray
{
    public static T CreateJaggedArray<T>(params int[] lengths)
    {
        return (T)InitializeJaggedArray(typeof(T).GetElementType(), 0, lengths);
    }

    private static object InitializeJaggedArray(Type type, int index, IReadOnlyList<int> lengths)
    {
        Array array = Array.CreateInstance(type, lengths[index]);
        Type elementType = type.GetElementType();

        if (elementType == null) 
            return array;

        for (int i = 0; i < lengths[index]; i++)
            array.SetValue(InitializeJaggedArray(elementType, index + 1, lengths), i);

        return array;
    }
}
