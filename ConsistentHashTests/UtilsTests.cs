using ConsistentHashing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsistentHashTests
{
    class UtilsTests
    {
        #region RadixSort tests
        [Test]
        public void RadixSortTest()
        {
            var sectors = GenerateUniqueSectors(amountOfValues: 100, amountOfSectorsPerValue: 100);
            var unsortedSectors = Shuffle(sectors);
            var sortedSectorsResult = Utils.RadixSort(unsortedSectors);
            var sortedSectorsExpected = unsortedSectors.OrderBy(item => item.m_endAngle).ToArray();
            Assert.AreEqual(sortedSectorsExpected, sortedSectorsResult);
        }

        [Test]
        public void RadixSortReverseTest()
        {
            var sectors = GenerateUniqueSectors(amountOfValues: 100, amountOfSectorsPerValue: 100);
            var reversedSectors = sectors.Reverse().ToArray();
            var sortedSectorsResult = Utils.RadixSort(reversedSectors);
            var sortedSectorsExpected = reversedSectors.OrderBy(item => item.m_endAngle).ToArray();
            Assert.AreEqual(sortedSectorsExpected, sortedSectorsResult);
        }

        [Test]
        public void RadixSortStableTest()
        {
            var sectors = GenerateSectorsWithDuplicates(amountOfValues: 100, amountOfAngleDuplicate: 10);
            var unsortedSectors = Shuffle(sectors).ToArray();
            var sortedByValues = unsortedSectors.OrderBy(sector => sector.m_node).ToArray();
            var sortedSectorsResult = Utils.RadixSort(sortedByValues);
            var sortedSectorsExpected = unsortedSectors.OrderBy(sector => sector.m_node).OrderBy(item => item.m_endAngle).ToArray();
            Assert.AreEqual(sortedSectorsExpected, sortedSectorsResult);
        }

        private static Sector<string>[] GenerateUniqueSectors(int amountOfValues, int amountOfSectorsPerValue)
        {
            return Enumerable.Range(0, amountOfValues)
                .SelectMany(valueIndex => Enumerable.Range(0, amountOfSectorsPerValue)
                    .Select(sectorIndex => new Sector<string>((uint)(valueIndex * amountOfSectorsPerValue + sectorIndex), valueIndex.ToString())))
                .ToArray();
        }

        private static Sector<string>[] GenerateSectorsWithDuplicates(int amountOfValues, int amountOfAngleDuplicate)
        {
            return Enumerable.Range(0, amountOfAngleDuplicate)
                .SelectMany(duplicateIndex => Enumerable.Range(0, amountOfValues)
                    .Select(valueIndex => new Sector<string>((uint)duplicateIndex, valueIndex.ToString())))
                .ToArray();
        }

        private static Sector<string>[] Shuffle(Sector<string>[] sectorArray)
        {
            Random rnd = new Random();
            return sectorArray.OrderBy(item => rnd.Next()).ToArray();
        }
        #endregion RadixSort tests

        #region XxHash32 tests
        [Test]
        public void XxHash32PrecalculatedValueTest()
        {
            var value = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("ABCD"));
            var hash = Utils.XxHash32(value);
            Assert.AreEqual(0xaa960ca6, hash, "value was different from precalculated value");
        }

        [Test]
        public void XxHash32PrecalculatedValueTestWithCustomSeed()
        {
            var value = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("aaaa"));
            var hash = Utils.XxHash32(value, 123);
            Assert.AreEqual(0x13e74606, hash, "value was different from precalculated value");
        }
        #endregion XxHash32 tests

        #region MergeSortedWithSorted tests
        [Test]
        public void MergeSortedWithSortedEdgeTest()
        {
            // Given..
            var sorted1 = Enumerable.Range(start: 0, count: 100).Select(index => new Sector<int>((uint)index, index)).ToArray();
            var sorted2 = Enumerable.Range(start: 100, count: 100).Select(index => new Sector<int>((uint)index, index)).ToArray();

            // When..
            var actual = Utils.MergeSortedWithSorted(sorted1, sorted2);
            var expected = Enumerable.Range(0, 200).Select(index => new Sector<int>((uint)index, index)).ToArray();

            // Then..
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [Test]
        public void MergeSortedWithSortedTest()
        {
            // Given..
            var sorted1 = Enumerable.Range(start: 0, count: 100).Select(index => index * 2)
                .Select(index => new Sector<int>((uint)index, index)).ToArray();
            var sorted2 = Enumerable.Range(start: 0, count: 100).Select(index => (index * 2) + 1)
                .Select(index => new Sector<int>((uint)index, index)).ToArray();

            // When..
            var actual = Utils.MergeSortedWithSorted(sorted1, sorted2);
            var expected = Enumerable.Range(0, 200).Select(index => new Sector<int>((uint)index, index)).ToArray();

            // Then..
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
        #endregion MergeSortedWithSorted tests
    }
}
