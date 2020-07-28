using System;
using System.Text;

namespace pocoGenerator
{
    /// <summary>
    /// Encapsulates StringBuilder procedures for exposing a class
    /// </summary>
    public class Classer: IDisposable
    {
        StringBuilder _sb;

        public Classer() { }

        /// <summary>
        /// Initialize a class declaration, with using, namespace and classname
        /// </summary>
        /// <param name="_nameSpace"></param>
        public Classer(string _nameSpace, string _className, string _inherits = "")
        {
            _sb = new StringBuilder("using System;\r\n")
                            .AppendLine("using System.Xml;")
                            .AppendLine("using System.Linq;")
                            .AppendLine("using System.Data.Entity;")
                            .AppendLine("using System.ComponentModel.DataAnnotations;")
                            .AppendLine("namespace " + _nameSpace)
                            .AppendLine("{")
                            .Append("\tpublic partial class " + _className)
                            .AppendLine(_inherits.CompareTo("") != 0 ? " : " + _inherits : "")
                            .AppendLine("\t{");
        }

        /// <summary>
        /// Closes class and namespace declaration
        /// </summary>
        public void Close()
        {
            _sb.AppendLine("\t}")
               .AppendLine("}");
        }

        public StringBuilder Append(string _text)
        {
            return _sb.Append(_text);
        }

        public StringBuilder AppendLine(string _text)
        {
            return _sb.AppendLine(_text);
        }

        public void AddDataAnnotation(string _dataAnnotation)
        {
            _sb.Append("\t\t")
               .AppendLine(_dataAnnotation);
        }

        public void AddPublicProperty(string _type, string _name)
        {
            _sb.Append("\t\tpublic ")
               .Append(_type)
               .Append(" ")
               .Append(_name)
               .AppendLine(" { get; set; }");
        }

        public void AddArgument(string _type, string _name, bool _addComma = true)
        {
            _sb.Append(_type)
               .Append(" ")
               .Append(_name.Replace("@", ""))
               .Append(_addComma ? ", " : "");
        }

        public void AddDbSet(string _name)
        {
            _sb.Append("\t\tpublic virtual DbSet<")
               .Append(_name)
               .Append("> ")
               .Append(_name)
               .AppendLine(" { get; set; }");
        }

        /// <summary>
        /// In calling ToString() method, expose the generated class
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _sb.ToString();
        }

        // ********************************************************************************************************************************************
        public void Dispose()
        {
            _sb?.Clear();
            _sb = null;
        }
    }
}
