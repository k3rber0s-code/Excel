using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Excel
{
    public class ErrorMessages
    {
        public const string InvalidInput = "#INVVAL";
        public const string MissingOperator = "#MISSOP"; 
        public const string Error = "#ERROR";
    }
    class ExcelTable
    {
        readonly char[] ops = { '+', '-', '/', '*' };
        readonly string operatorSplitPattern = @"(?<=[()+*/-])|(?=[()+*/-])";
        bool[][] evaluatedCells;
        List<Position> cellsToEvaluate;
        public ExcelTable(string path)
        {
            cellsToEvaluate = new List<Position>();
            LoadTableFromFile(path);
        }
        string[][] LoadTableFromFile(string path)
        {
            List<string[]> rows = new List<string[]>();
            List<string[]> currentlyEvaluatedCells = new List<string[]>();
            int rowCounter = 0;

            using (StreamReader sr = new StreamReader(path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] currentRow = line.Split(' ');
                    bool[] currentlyEvaluatedRow = new bool[currentRow.Length];

                    for (int i = 0; i < currentRow.Length; i++)
                    {
                        string result;
                        if (!Evaluate(currentRow[i], out result))
                        {
                            currentlyEvaluatedRow[i] = false;
                            cellsToEvaluate.Add(new Position(rowCounter, i));
                        }
                        else
                        {
                            currentlyEvaluatedRow[i] = true;
                            currentRow[i] = result;
                        }
                    }
                    rows.Add(currentRow);
                    rowCounter++;
                }
            }
            string[][] table = rows.ToArray();
            return table;
        }

        private bool Evaluate(string cell, out string result)
        {
            if (GetCellType(cell) == CellType.INVALID)
            {
                result = ErrorMessages.InvalidInput;
                return true;
            }
            switch (GetCellType(cell))
            {
                case CellType.INVALID:
                    result = ErrorMessages.InvalidInput;
                    return true;
                case CellType.NUMBER:
                    result = cell;
                    return true;
                case CellType.EMPTY:
                    result = cell;
                    return true;
                case CellType.FUNCTION:
                    return EvaluateFunction(cell, out result);
                default:
                    break;
            }
            result = cell;
            return false;
        }

        private bool EvaluateFunction(string function, out string result)
        {
            Regex f = new Regex(operatorSplitPattern);
            string[] tokens = f.Split(function.Substring(1, function.Length -1));

            if (IsMissingOperator(function)) // case "=[cellNumber]" ...tokens.Length > 1 &&.. 
            {
                result = ErrorMessages.MissingOperator;
                return true;
            }
            else
            {
                bool CanBeEvaluated = true;
                StringBuilder expression = new StringBuilder();
                foreach(var token in tokens)
                {
                    if (IsOperator(token[0]))
                    {
                        if (token.Length > 1 || expression.Length == 0) // needs to be valid operator; function cannot begin with an operator
                        {
                            result = ErrorMessages.Error;
                            return true;
                        }
                        else
                        {
                            expression.Append(token);
                        }
                    }
                    else
                    {
                        ErrorType error;
                        Position p = GetPositonFromEncoding(token, out error);
                        if (error == ErrorType.NONE)
                        {
                        }
                    }
                }
                result = function;
                return false;
            }
        }

        private bool IsOperator(char c)
        {
            return (ops.Contains(c));
        }


        private bool IsMissingOperator(string function)
        {
            foreach(char op in ops) {
                if (!function.Contains(op))
                {
                    return false;
                }
            }
            return true;
        }

        private Position GetPositonFromEncoding (string encoding, out ErrorType error)
        {
            Regex re = new Regex(@"([a-zA-Z]+)(\d+)");
            if (!re.IsMatch(encoding))
            {
                error = ErrorType.FORMULA;
                return new Position(-1, -1);
            }
            Match result = re.Match(encoding);

            string alphaPart = result.Groups[1].Value;
            string numberPart = result.Groups[2].Value;

            int row = Int32.Parse(numberPart);
            int column = GetColumnNumber(alphaPart);

            error = ErrorType.NONE;
            return new Position(row, column);
        }
        // ABC -> 
        private int GetColumnNumber(string alphaPart)
        {
            int result = 0;
            alphaPart = (alphaPart.ToCharArray().Reverse()).ToString();
            for (int i = 0; i < alphaPart.Length; i++)
            {
                result += ((alphaPart[i] - 64)* (int)Math.Pow(26, i));
            }
            return (result - 1); // indexing from 0
        }

        private CellType GetCellType(string cell)
        {
            int num;
            if (Int32.TryParse(cell, out num))
            {
                return CellType.NUMBER;
            }
            else if (cell[0] == '=')
            {
                return CellType.FUNCTION;
            }
            else if (cell == "[]")
            {
                return CellType.EMPTY;
            }
            else
            {
                return CellType.INVALID;
            }
        }
    }
    public enum ErrorType
    {
        NONE,
        ERROR,
        DIV0,
        CYCLE,
        MISSOP,
        FORMULA
    }
    public enum CellType
    {
        NUMBER,
        FUNCTION,
        EMPTY,
        INVALID
    }

    public struct Position
    {
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }
        public int X { get; }
        public int Y { get; }

        public override string ToString() => $"({X}, {Y})";
    }
}
