using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.InfraredKinectData
{
    public static class TransposeRowsColumnsExtension
    {
        [CLSCompliant(false)]
        /// <summary>
        /// Transposes the rows and columns of a two-dimensional array
        /// </summary>
        /// <typeparam name="T">The type of the items in the array</typeparam>
        /// <param name="arr">The array</param>
        /// <returns>A new array with rows and columns transposed</returns>
        public static T[,] TransposeRowsAndColumns<T>(this T[,] arr)
        {
            int rowCount = arr.GetLength(0);
            int columnCount = arr.GetLength(1);
            T[,] transposed = new T[columnCount, rowCount];
            if (rowCount == columnCount)
            {
                transposed = (T[,])arr.Clone();
                for (int i = 1; i < rowCount; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        T temp = transposed[i, j];
                        transposed[i, j] = transposed[j, i];
                        transposed[j, i] = temp;
                    }
                }
            }
            else
            {
                for (int column = 0; column < columnCount; column++)
                {
                    for (int row = 0; row < rowCount; row++)
                    {
                        transposed[column, row] = arr[row, column];
                    }
                }
            }
            return transposed;
        }

        /// <summary>
        /// Transposes the rows and columns of a jagged array
        /// </summary>
        /// <typeparam name="T">The type of the items in the array</typeparam>
        /// <param name="arr">The array</param>
        /// <returns>A new array with rows and columns transposed</returns>
        public static T[][] TransposeRowsAndColumns<T>(this T[][] arr)
        {
            int rowCount = arr.Length;
            int columnCount = arr[0].Length;
            T[][] transposed = new T[columnCount][];
            if (rowCount == columnCount)
            {
                transposed = (T[][])arr.Clone();
                for (int i = 1; i < rowCount; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        T temp = transposed[i][j];
                        transposed[i][j] = transposed[j][i];
                        transposed[j][i] = temp;
                    }
                }
            }
            else
            {
                for (int column = 0; column < columnCount; column++)
                {
                    transposed[column] = new T[rowCount];
                    for (int row = 0; row < rowCount; row++)
                    {
                        transposed[column][row] = arr[row][column];
                    }
                }
            }
            return transposed;
        }

        /// <summary>
        /// Transposes the rows and columns of a string
        /// </summary>
        /// <param name="str">The string</param>
        /// <param name="rowDelimiter">The delimiter of the rows</param>
        /// <param name="columnDelimiter">The delimiter of the columns</param>
        /// <returns>A new string with rows and columns transposed</returns>
        public static string TransposeRowsAndColumns(this string str, string rowDelimiter, string columnDelimiter)
        {
            string[] rows = str.Split(new string[] { rowDelimiter }, StringSplitOptions.None);
            string[][] arr = new string[rows.Length][];
            for (int i = 0; i < rows.Length; i++)
            {
                arr[i] = rows[i].Split(new string[] { columnDelimiter }, StringSplitOptions.None);
            }
            string[][] transposed = TransposeRowsAndColumns(arr);
            string[] transposedRows = new string[transposed.Length];
            for (int i = 0; i < transposed.Length; i++)
            {
                transposedRows[i] = String.Join(columnDelimiter, transposed[i]);
            }
            return String.Join(rowDelimiter, transposedRows);
        }
    }
}
