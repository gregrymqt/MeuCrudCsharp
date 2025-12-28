using System;

namespace MeuCrudCsharp.Documents.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MongoIndexAttribute : Attribute
    {
        public bool Unique { get; set; } = false;
        public bool Descending { get; set; } = false;
    }
}
