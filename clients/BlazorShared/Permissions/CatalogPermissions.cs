namespace FSH.BlazorShared.Permissions;

public static class CatalogPermissions
{
    public static class Products
    {
        public const string View = "Permissions.Catalog.Products.View";
        public const string Create = "Permissions.Catalog.Products.Create";
        public const string Update = "Permissions.Catalog.Products.Update";
        public const string Delete = "Permissions.Catalog.Products.Delete";
        public const string Restore = "Permissions.Catalog.Products.Restore";
    }

    public static class Brands
    {
        public const string View = "Permissions.Catalog.Brands.View";
        public const string Create = "Permissions.Catalog.Brands.Create";
        public const string Update = "Permissions.Catalog.Brands.Update";
        public const string Delete = "Permissions.Catalog.Brands.Delete";
        public const string Restore = "Permissions.Catalog.Brands.Restore";
    }

    public static class Categories
    {
        public const string View = "Permissions.Catalog.Categories.View";
        public const string Create = "Permissions.Catalog.Categories.Create";
        public const string Update = "Permissions.Catalog.Categories.Update";
        public const string Delete = "Permissions.Catalog.Categories.Delete";
        public const string Restore = "Permissions.Catalog.Categories.Restore";
    }
}
