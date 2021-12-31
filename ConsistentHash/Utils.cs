using System;
using System.Collections.Generic;

namespace ConsistentHashing
{
    internal class Utils
    {
        private const int numberOfBitsPerIteration = 8;
        private const int numberOfBucketsPerIteration = 1 << numberOfBitsPerIteration;
        internal static Sector<T>[] RadixSort<T>(Sector<T>[] sectors)
        {
            var nextIterationSectors = new Sector<T>[sectors.Length];
            var buckets = new uint[numberOfBucketsPerIteration];

            uint bitsFilter = numberOfBucketsPerIteration - 1;
            var numOfBitsShifted = 0;

            while (bitsFilter != 0)
            {
                // Set all buckets to zeros.
                if (numOfBitsShifted != 0) // if not first iteration
                {
                    Array.Clear(buckets, 0, buckets.Length);
                }
                // Apply counting sort over unfiltered bits.
                for (var i = 0; i < sectors.Length; i++)
                {
                    buckets[(sectors[i].m_endAngle & bitsFilter) >> numOfBitsShifted]++;
                }
                // Aggregate the counting sort.
                for (var i = 1; i < buckets.Length; i++)
                {
                    buckets[i] += buckets[i - 1];
                }

                for (var i = sectors.Length - 1; i >= 0; i--)
                {
                    nextIterationSectors[--buckets[(sectors[i].m_endAngle & bitsFilter) >> numOfBitsShifted]] = sectors[i];
                }

                bitsFilter <<= numberOfBitsPerIteration;
                numOfBitsShifted += numberOfBitsPerIteration;

                var tmp = sectors; // swap input and output arrays
                sectors = nextIterationSectors;
                nextIterationSectors = tmp;
            }
            return sectors;
        }

        internal static Comparer<Sector<T>> GetSectorComparer<T>(IComparer<T> comparer)
        {
            return Comparer<Sector<T>>.Create((Sector<T> x, Sector<T> y) =>
            {
                var value = x.m_endAngle.CompareTo(y.m_endAngle);
                if (value != 0)
                {
                    return value;
                }

                var xIsDefault = (x.m_node == null) || x.m_node.Equals(default);
                var yIsDefault = (y.m_node == null) || y.m_node.Equals(default);
                if (xIsDefault && yIsDefault)
                {
                    return 0;
                }
                else if (xIsDefault)
                {
                    return -1;
                }
                else if (yIsDefault)
                {
                    return 1;
                }
                else
                {
                    return comparer.Compare(x.m_node, y.m_node);
                }
            });
        }

        internal static Sector<T>[] MergeSortedWithSorted<T>(Sector<T>[] sortedSectors1, Sector<T>[] sortedSectors2, Comparer<Sector<T>> comparer)
        {
            var result = new Sector<T>[sortedSectors1.Length + sortedSectors2.Length];
            var index1 = 0;
            var index2 = 0;
            var resultIndex = 0;
            while (resultIndex < result.Length)
            {
                if (sortedSectors1.Length == index1)
                {
                    result[resultIndex++] = sortedSectors2[index2++];
                }
                else if (sortedSectors2.Length == index2)
                {
                    result[resultIndex++] = sortedSectors1[index1++];
                }
                else if (comparer.Compare(sortedSectors1[index1], sortedSectors2[index2]) > 0)
                {
                    result[resultIndex++] = sortedSectors2[index2++];
                }
                else
                {
                    result[resultIndex++] = sortedSectors1[index1++];
                }
            }
            return result;
        }

        private const uint primeNum2 = 2246822519U;
        private const uint primeNum3 = 3266489917U;
        private const uint primeNum4 = 668265263U;
        private const uint primeNum5 = 374761393U;

        internal static uint XxHash32(uint value, uint seed = 0)
        {
            var hash = sizeof(uint) + primeNum5;
            hash += seed;
            hash += value * primeNum3;
            hash = ((hash << 17) | (hash >> (32 - 17)));
            hash *= primeNum4;
            hash ^= hash >> 15;
            hash *= primeNum2;
            hash ^= hash >> 13;
            hash *= primeNum3;
            hash ^= hash >> 16;
            return hash;
        }
    }
}
