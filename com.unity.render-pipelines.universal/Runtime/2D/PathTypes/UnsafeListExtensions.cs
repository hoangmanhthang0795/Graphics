using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class UnsafeListExtension
{
    public static void Reverse<T>(this UnsafeList<T> list, int index, int count) where T : unmanaged
    {
        int halfCount = count >> 1;
        for (int i = 0; i < halfCount; i++)
        {
            int startIndex = index + i;
            int endIndex = index + count - 1 - i;

            // Swap
            T tempValue = list[startIndex];
            list[startIndex] = list[endIndex];
            list[endIndex] = tempValue;
        }
    }

    public static void Reverse<T>(this UnsafeList<T> list) where T : unmanaged
    {
        list.Reverse(0, list.Length);
    }

    public static void Insert<T>(this UnsafeList<T> list, int index, T item) where T : unmanaged
    {
        list.Add(default(T));

        // Copy items
        for(int i = list.Length-1; i > index ; i--)
            list[i] = list[i - 1];

        list[index] = item;
    }

    public static void AddRange<T>(this UnsafeList<T> list, UnsafeList<T> rangeValues) where T : unmanaged
    {
        for (int i = rangeValues.Length - 1; i >= 0; i--)
            list.Add(rangeValues[i]);
    }

    public static UnsafeList<T> GetRange<T>(this UnsafeList<T> list, int index, int count) where T : unmanaged
    {
        UnsafeList<T> rangeValues = new UnsafeList<T>(count, Allocator.Temp, NativeArrayOptions.ClearMemory);

        for (int i = count - 1; i >= 0; i--)
            rangeValues.Add(list[index + i]);

        return rangeValues;
    }

    public static ref T GetIndexByRef<T>(this UnsafeList<T> list, int index) where T : unmanaged
    {
        unsafe
        {
            return ref list.Ptr[index];
        }
    }
}
