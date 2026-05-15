using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Nanchesoft.Persistence.Context;

public static class ModelBuilderSnakeCaseExtensions
{
    public static void UseNanchesoftSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            var schema = entity.GetSchema();
            if (!string.IsNullOrWhiteSpace(schema))
            {
                entity.SetSchema(ToSnakeCase(schema));
            }

            var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject) ?? property.Name;
                property.SetColumnName(ToSnakeCase(columnName));
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(constraintName))
                {
                    foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(indexName))
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);
        UnicodeCategory? previousCategory = null;

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (current == '_' || current == '-' || current == ' ')
            {
                if (builder.Length > 0 && builder[^1] != '_')
                {
                    builder.Append('_');
                }

                previousCategory = null;
                continue;
            }

            var currentCategory = char.GetUnicodeCategory(current);

            if (currentCategory == UnicodeCategory.UppercaseLetter)
            {
                var hasPrevious = previousCategory.HasValue && previousCategory != UnicodeCategory.SpaceSeparator;
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                var previousIsLowerOrDigit = previousCategory is UnicodeCategory.LowercaseLetter or UnicodeCategory.DecimalDigitNumber;

                if (builder.Length > 0 && builder[^1] != '_' && (previousIsLowerOrDigit || (hasPrevious && nextIsLower)))
                {
                    builder.Append('_');
                }

                current = char.ToLowerInvariant(current);
            }
            else
            {
                current = char.ToLowerInvariant(current);
            }

            builder.Append(current);
            previousCategory = currentCategory;
        }

        return builder.ToString().Trim('_');
    }
}
