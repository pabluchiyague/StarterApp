namespace RentalApp.ViewModels
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class QueryPropertyAttribute : Attribute
    {
        public QueryPropertyAttribute(string name, string queryId)
        {
            Name = name;
            QueryId = queryId;
        }

        public string Name { get; }
        public string QueryId { get; }
    }
}

namespace RentalApp.Views
{
    public sealed class CreateItemPage;

    public sealed class EditItemPage;

    public sealed class ItemDetailPage;

    public sealed class UserProfilePage;
}
