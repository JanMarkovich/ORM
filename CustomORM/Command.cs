using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CustomORM
{
    /// <summary>
    ///     Associate classes or properties with sql objects
    /// </summary>
    public class SqlObjectNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public SqlObjectNameAttribute(string name)
        {
            Name = name;
        }
    }


    /// <summary>
    ///     Translate Sql object into Models
    /// </summary>
    public class SqlObjectToModelCorrespondingAttribute : Attribute
    {
        public string Name { get; private set; }

        public SqlObjectToModelCorrespondingAttribute(string name)
        {
            Name = name;
        }
    }


    /// <summary>
    ///     Means that property not assosiated with sql object
    /// </summary>
    public class NotSqlObjectAttribute : Attribute
    {
    }


    /// <summary>
    ///     Types of SQL JOIN command
    /// </summary>
    public enum TypesOfJoin
    {
        Inner,
        Left,
        Right,
        Full,
        Cross
    }

    /// <summary>
    ///     Class for creating SQL command with CSharp language
    /// </summary>
    public class CustomSqlCommand
    {
        private string _connectionString;

        private string _select;
        private string _from;
        private string _where;
        private string _join;

        // Corresponding between parameters in lambda with their sql tables names
        private Dictionary<ParameterExpression, string> _lambdaParamsToNamesOfSqlTablesDictionary;

        // Corresponds lambda parameters with dictionary that corresponds properties of classes that represented by lambda parameters and their SQL columns names
        private Dictionary<ParameterExpression, Dictionary<string, string>> _propertiesOfLambdaParamsToNamesOfSqlColumns;


        private enum TypesOfWhere
        {
            Where,
            And,
            Or
        }


        /// <summary>
        ///     Convert CustomSqlCommand into string representative of SQL command
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _select + _from + _join + _where;
        }


        public CustomSqlCommand(string connectionName)
        {
            _connectionString = ConfigurationManager.ConnectionStrings[connectionName].ToString();
            Initialize();
        }


        private void Initialize()
        {
            _select = _from = _join = _where = "";
            _lambdaParamsToNamesOfSqlTablesDictionary = new Dictionary<ParameterExpression, string>();
            _propertiesOfLambdaParamsToNamesOfSqlColumns =
                new Dictionary<ParameterExpression, Dictionary<string, string>>();
        }

        public CustomSqlCommand Select(params Type[] typesOfClasses)
        {
            return Select((IEnumerable<Type>)typesOfClasses);
        }

        /// <summary>
        ///     Select
        /// </summary>
        /// <param name="typesOfClasses"></param>
        /// <returns></returns>
        public CustomSqlCommand Select(IEnumerable<Type> typesOfClasses)
        {
            _select = "SELECT ";
            foreach (var typeOfClass in typesOfClasses)
            {
                var propertiesToSqlColumnsNamesDictionary = GetPropertiesToSqlColumnsNamesDictionary(typeOfClass);
                var tableName = GetSqlTableName(typeOfClass);
                foreach (var value in propertiesToSqlColumnsNamesDictionary.Values)
                {
                    _select += tableName + "." + value + " as '" + tableName + "." + value + "'" + ", ";
                }
            }
            _select = _select.Remove(_select.Length - 2);

            return this;
        }


        public CustomSqlCommand From(params Type[] typesOfClasses)
        {
            return From((IEnumerable<Type>)typesOfClasses);
        }


        public CustomSqlCommand From(IEnumerable<Type> typesOfClasses)
        {
            if (_from == "")
            {
                _from = " FROM ";
                foreach (var typeOfClass in typesOfClasses)
                {
                    _from += GetSqlTableName(typeOfClass) + ", ";
                }
                _from = _from.Remove(_from.Length - 2);
            }
            else
            {
                var currentFrom = typesOfClasses.Select(GetSqlTableName).ToList();
                // remove "from" then split by ',' and delete first spaces in all strings
                var oldFrom = _from.Remove(0, 4).Split(',').Select(str => str.Remove(0, 1)).ToList();
                // last table in statment "from" need for join
                var lastTable = currentFrom.Last();

                var newFrom = currentFrom.Union(oldFrom).ToList();
                newFrom.Remove(lastTable);
                newFrom.Add(lastTable);

                _from = " FROM ";
                foreach (var tableName in newFrom)
                {
                    _from += tableName + ", ";
                }
                _from = _from.Remove(_from.Length - 2);
            }

            return this;
        }


        /// <summary>
        ///     Join T1 with T2 by pattern: ... T1 [] JOIN T2 ON ...
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="join"></param>
        /// <param name="typesOfJoin"></param>
        /// <returns></returns>
        public CustomSqlCommand Join<T1, T2>(Expression<Func<T1, T2, bool>> join,
            TypesOfJoin typesOfJoin = TypesOfJoin.Inner)
        {
            foreach (var parameterExpression in join.Parameters)
            {
                _lambdaParamsToNamesOfSqlTablesDictionary.Add(parameterExpression,
                    GetSqlTableName(parameterExpression.Type));
                _propertiesOfLambdaParamsToNamesOfSqlColumns.Add(parameterExpression,
                    GetPropertiesToSqlColumnsNamesDictionary(parameterExpression.Type));
            }

            _join +=
                string.Format(
                    " {0} JOIN " + _lambdaParamsToNamesOfSqlTablesDictionary[join.Parameters[1]] + " ON " +
                    PerformLambdaIntoSqlCommand(join.Body), typesOfJoin.ToString().ToUpper());

            _lambdaParamsToNamesOfSqlTablesDictionary = new Dictionary<ParameterExpression, string>();
            _propertiesOfLambdaParamsToNamesOfSqlColumns = new Dictionary<ParameterExpression, Dictionary<string, string>>();
            return this;
        }


        private void WhereBase<T>(Expression<T> where, TypesOfWhere whereType = TypesOfWhere.Where)
        {
            foreach (var parameterExpression in where.Parameters)
            {
                _lambdaParamsToNamesOfSqlTablesDictionary.Add(parameterExpression,
                    GetSqlTableName(parameterExpression.Type));
                _propertiesOfLambdaParamsToNamesOfSqlColumns.Add(parameterExpression,
                    GetPropertiesToSqlColumnsNamesDictionary(parameterExpression.Type));
            }

            _where += string.Format(" {0} {1}", whereType, PerformLambdaIntoSqlCommand(where.Body));

            _lambdaParamsToNamesOfSqlTablesDictionary = new Dictionary<ParameterExpression, string>();
            _propertiesOfLambdaParamsToNamesOfSqlColumns = new Dictionary<ParameterExpression, Dictionary<string, string>>();
        }


        public CustomSqlCommand Where<T1>(Expression<Func<T1, bool>> where)
        {
            WhereBase(where);
            return this;
        }

        public CustomSqlCommand Where<T1, T2>(Expression<Func<T1, T2, bool>> where)
        {
            WhereBase(where);
            return this;
        }


        public CustomSqlCommand AndWhere<T1, T2>(Expression<Func<T1, T2, bool>> where)
        {
            WhereBase(where, TypesOfWhere.And);
            return this;
        }


        public CustomSqlCommand OrWhere<T1, T2>(Expression<Func<T1, T2, bool>> where)
        {
            WhereBase(where, TypesOfWhere.Or);
            return this;
        }


        private string GetSqlTableName(Type typeForSqlObject)
        {
            var classSqlObjectNameAttributes = Attribute.GetCustomAttributes(typeForSqlObject,
                typeof (SqlObjectNameAttribute));
            return classSqlObjectNameAttributes.Any()
                ? ((SqlObjectNameAttribute) classSqlObjectNameAttributes[0]).Name
                : typeForSqlObject.Name;
        }


        private Dictionary<string, string> GetPropertiesToSqlColumnsNamesDictionary(Type typeForSqlObject)
        {
            var propertiesToSqlColumnsNamesDictionary = new Dictionary<string, string>();

            foreach (var property in typeForSqlObject.GetProperties())
            {
                var propertyNotSqlObjectAttributes = Attribute.GetCustomAttributes(property,
                    typeof (NotSqlObjectAttribute));
                if (propertyNotSqlObjectAttributes.Any())
                {
                    continue;
                }

                var propertySqlObjectNameAttributes = Attribute.GetCustomAttributes(property,
                    typeof (SqlObjectNameAttribute));
                if (propertySqlObjectNameAttributes.Any())
                {
                    propertiesToSqlColumnsNamesDictionary.Add(property.Name,
                        ((SqlObjectNameAttribute) propertySqlObjectNameAttributes[0]).Name);
                    continue;
                }

                propertiesToSqlColumnsNamesDictionary.Add(property.Name, property.Name);
            }

            return propertiesToSqlColumnsNamesDictionary;
        }


        private string PerformLambdaIntoSqlCommand(object expression)
        {
            var str = "";

            if (expression is BinaryExpression)
            {
                var leftPartOfExpression = ((BinaryExpression) expression).Left;
                var rightPartOfExpression = ((BinaryExpression) expression).Right;
                var operation = ((BinaryExpression) expression).NodeType;

                str += "(" + PerformLambdaIntoSqlCommand(leftPartOfExpression) + ")";

                str += " " + ArithmeticOperatorsDictionary.GetSqlOperation(operation.ToString()) + " ";

                str += "(" + PerformLambdaIntoSqlCommand(rightPartOfExpression) + ")";
            }
            else
            {
                // try convert expression to SQL objects
                string[] lambdaParameterAndProperty = null;
                if (expression is UnaryExpression)
                {
                    lambdaParameterAndProperty = ((UnaryExpression)expression).Operand.ToString().Split('.');
                }
                else
                {
                    lambdaParameterAndProperty = expression.ToString().Split('.');
                }
                var lambdaParamToNameOfSqlTable =
                    _lambdaParamsToNamesOfSqlTablesDictionary.FirstOrDefault(
                        x => x.Key.ToString() == lambdaParameterAndProperty[0]);

                if (lambdaParamToNameOfSqlTable.Value != null)
                {
                    var operand = lambdaParamToNameOfSqlTable.Value;
                    if (lambdaParameterAndProperty.Length == 2)
                    {
                        operand += "." +
                                   _propertiesOfLambdaParamsToNamesOfSqlColumns[lambdaParamToNameOfSqlTable.Key]
                                       .FirstOrDefault(x => x.Key.ToString() == lambdaParameterAndProperty[1]).Value;
                    }
                    return operand;
                }

                // if expression can be invoked
                var result = Expression.Lambda((Expression) expression).Compile().DynamicInvoke();
                if (result is string)
                {
                    return "'" + result + "'";
                }
                if (result != null)
                {
                    return result.ToString();
                }

                return expression.ToString();
            }

            return str;
        }


        public IEnumerable<T> ExecuteCommand<T>()
        {
            var typeOfModel = typeof(T);
            var tableName = GetSqlTableName(typeOfModel);

            var dataSet = new DataSet();
            var adapter = new SqlDataAdapter
            {
                SelectCommand = new SqlCommand(ToString(), new SqlConnection(_connectionString))
            };
            adapter.Fill(dataSet);

            // get dictionary Headers-Properties
            var headersInTable = dataSet.Tables[0].Columns;
            var propertiesInModel = typeOfModel.GetProperties();
            var fromHeadersToPropertiesDictionary = new Dictionary<string, PropertyInfo>();

            for (var i = 0; i < headersInTable.Count; i++)
            {
                foreach (var property in propertiesInModel)
                {
                    if (Attribute.GetCustomAttributes(property, typeof(NotSqlObjectAttribute)).Any())
                    {
                        continue;
                    }

                    var propertyAttributes = Attribute.GetCustomAttributes(property, typeof(SqlObjectToModelCorrespondingAttribute));

                    if (propertyAttributes.Any())
                    {
                        if (String.Equals(((SqlObjectToModelCorrespondingAttribute)propertyAttributes[0]).Name, headersInTable[i].ToString(), StringComparison.CurrentCultureIgnoreCase))
                        {
                            fromHeadersToPropertiesDictionary.Add(headersInTable[i].ToString(), property);
                        }
                        continue;
                    }

                    if (String.Equals(tableName + "." + property.Name, headersInTable[i].ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        fromHeadersToPropertiesDictionary.Add(headersInTable[i].ToString(), property);
                    }
                }
            }
            ///////////////////////////

            var listOfData = new List<T>();

            // fill list with data from table
            for (var i = 0; i < dataSet.Tables[0].Rows.Count; i++)
            {
                var obj = Activator.CreateInstance(typeOfModel);

                for (var j = 0; j < headersInTable.Count; j++)
                {
                    if (fromHeadersToPropertiesDictionary.Keys.FirstOrDefault(x => x == headersInTable[j].ColumnName) !=
                        null)
                    {
                        var propertyForCurrentValue = fromHeadersToPropertiesDictionary[headersInTable[j].ColumnName];

                        if (dataSet.Tables[0].Rows[i][j] is DBNull)
                        {
                            propertyForCurrentValue.SetValue(obj, null);
                        }
                        else
                        {
                            propertyForCurrentValue.SetValue(obj,
                                Convert.ChangeType(dataSet.Tables[0].Rows[i][j], propertyForCurrentValue.PropertyType));
                        }
                    }
                }
                listOfData.Add((T)obj);
            }
            /////////////////////////////

            return listOfData;
        }
    }


    internal class ArithmeticOperatorsDictionary
    {
        private static readonly Dictionary<string, string> _arithmeticOperatorsDictionary;


        static ArithmeticOperatorsDictionary()
        {
            _arithmeticOperatorsDictionary = new Dictionary<string, string>()
            {
                {"Add", "+"},
                {"Subtract", "-"},
                {"Multiply", "*"},
                {"Divide", "/"},
                {"Modulo", "%"},
                {"And", "&"},
                {"Or", "|"},
                {"ExclusiveOr", "^"},
                {"Equal", "="},
                {"GreaterThan", ">"},
                {"LessThan", "<"},
                {"GreaterThanOrEqual", ">="},
                {"LessThanOrEqual", "<="},
                {"NotEqual", "<>"},
                {"AndAlso", "AND"},
                {"OrElse", "OR"},
                {"Not", "NOT"}
            };
        }


        public static string GetSqlOperation(string operation)
        {
            return _arithmeticOperatorsDictionary[operation];
        }
    }
}


/*public CustomSqlCommand Where<T1>(Expression<Func<T1, bool>> where)
        {
            foreach (var parameterExpression in where.Parameters)
            {
                _lambdaParamsToNamesOfSqlTablesDictionary.Add(parameterExpression,
                    GetSqlTableName(parameterExpression.Type));
                _propertiesOfLambdaParamsToNamesOfSqlColumns.Add(parameterExpression,
                    GetPropertiesToSqlColumnsNamesDictionary(parameterExpression.Type));
            }

            _where = " WHERE " + PerformLambdaIntoSqlCommand(where.Body);

            _lambdaParamsToNamesOfSqlTablesDictionary = new Dictionary<ParameterExpression, string>();
            _propertiesOfLambdaParamsToNamesOfSqlColumns = new Dictionary<ParameterExpression, Dictionary<string, string>>();
            return this;
        }


        public CustomSqlCommand Where<T1, T2>(Expression<Func<T1, T2, bool>> where)
        {
            foreach (var parameterExpression in where.Parameters)
            {
                _lambdaParamsToNamesOfSqlTablesDictionary.Add(parameterExpression,
                    GetSqlTableName(parameterExpression.Type));
                _propertiesOfLambdaParamsToNamesOfSqlColumns.Add(parameterExpression,
                    GetPropertiesToSqlColumnsNamesDictionary(parameterExpression.Type));
            }

            _where = " WHERE " + PerformLambdaIntoSqlCommand(where.Body);

            _lambdaParamsToNamesOfSqlTablesDictionary = new Dictionary<ParameterExpression, string>();
            _propertiesOfLambdaParamsToNamesOfSqlColumns = new Dictionary<ParameterExpression, Dictionary<string, string>>();
            return this;
        }*/