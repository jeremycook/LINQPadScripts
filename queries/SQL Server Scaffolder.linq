<Query Kind="Program">
  <Connection>
    <ID>f02d73bf-b25e-4e63-96a4-ff955ee4ec44</ID>
    <Persist>true</Persist>
    <Server>localhost\SQLEXPRESS</Server>
    <IncludeSystemObjects>true</IncludeSystemObjects>
    <Database>Connect</Database>
    <ShowServer>true</ShowServer>
  </Connection>
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>Humanizer</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>Humanizer</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Data.Common</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	var options = new ScaffoldOptions
	{
		Namespace = $"{Connection.Database}.Data",
		Context = $"{Connection.Database}DbBase",
		OutDir = Util.ReadLine<string>("OutDir"),
	};

	var schema = await new DbSchemaBuilder(DbSchema.Defaults.BuiltInTypes, DbSchema.Defaults.ClrTypes).CreateFromConnectionAsync(Connection);
	schema.Tables.RemoveAll(o => o.TableName == "sysdiagrams");

	var fileContents = new List<FileContents>();

	// Context
	var context = new StringBuilder();

	context.Append(
$@"using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace {options.Namespace}
{{
    public abstract class {options.Context} : DbContext
    {{
        public {options.Context}(DbContextOptions options)
            : base(options)
        {{
        }}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {{
            modelBuilder.ApplyConfigurationsFromAssembly(typeof({options.Context}).Assembly);
		}}

");

	foreach (var table in schema.Tables)
	{
		context.Append(
$@"        public virtual DbSet<{table.TableName}> {table.TableName} {{ get; set; }}
");
	}

	context.Append(
$@"    }}
}}
");

	fileContents.Add(new FileContents
	{
		Filename = $"{options.Context}.Generated.cs",
		Contents = context.ToString(),
	});


	// Tables
	foreach (var table in schema.Tables)
	{
		var tableContent = new StringBuilder();
		tableContent.Append(
$@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace {options.Namespace}
{{
    [Display(Name = ""{table.SingularName}"", GroupName = ""{table.PluralName}"")]
    public partial class {table.TableName}
    {{
");
		foreach (var column in table.Columns)
		{
			if (!column.IsNullable)
			{
				tableContent.Append(
$@"        [Required]
");
			}
			tableContent.Append(
$@"        [Display(Name = ""{column.SingularName}"", Order = {column.ColumnPosition})]
        public {column.ClrTypeName} {column.ColumnName} {{ get; set; }}
");

			var fk = schema.Relationships.SingleOrDefault(r =>
				r.ForeignSchemaName == table.SchemaName &&
				r.ForeignTableName == table.TableName &&
				//r.DeleteRule != DbRelationshipRule.Cascade &&
				r.Columns.Count == 1 &&
				r.Columns.Any(c => c.ForeignColumnName == column.ColumnName)
			);
			if (fk != null)
			{
				tableContent.Append(
$@"        [Display(Name = ""{column.SingularName}"", Order = {column.ColumnPosition})]
        public virtual {fk.PrimaryTableName} {Regex.Replace(column.ColumnName, "id$", "", RegexOptions.IgnoreCase)} {{ get; protected set; }}
");
			}

			if (table.Columns.Last() != column)
			{
				tableContent.AppendLine();
			}
		}

		var children = schema.Relationships.Where(r =>
			r.PrimarySchemaName == table.SchemaName &&
			r.PrimaryTableName == table.TableName &&
			//r.DeleteRule == DbRelationshipRule.Cascade &&
			r.Columns.Count == 1
		).ToList();
		foreach (var rel in children)
		{
			var foreignTable = schema.Tables.Single(t => t.SchemaName == rel.ForeignSchemaName && t.TableName == rel.ForeignTableName);
			if (rel.DeleteRule == DbRelationshipRule.Cascade)
			{
				tableContent.AppendLine($@"
        [DataType(""{foreignTable.TableName}List"")]
        [Display(Name = ""{foreignTable.PluralName}"")]
        public virtual List<{foreignTable.TableName}> {foreignTable.TableName} {{ get; set; }} = new List<{foreignTable.TableName}>();");
			}
			else
			{
				tableContent.AppendLine($@"
        [DataType(""{foreignTable.TableName}List"")]
        [Display(Name = ""{foreignTable.PluralName}"")]
        public virtual List<{foreignTable.TableName}> {(foreignTable == table ? "Children" : foreignTable.TableName)} {{ get; protected set; }}");
			}
		}

		var fks = schema.Relationships.Where(r =>
			r.ForeignSchemaName == table.SchemaName &&
			r.ForeignTableName == table.TableName &&
			//r.DeleteRule == DbRelationshipRule.Cascade &&
			r.Columns.Count > 1
		).ToList();
		foreach (var rel in fks)
		{
			var foreignTable = schema.Tables.Single(t => t.SchemaName == rel.ForeignSchemaName && t.TableName == rel.ForeignTableName);
			tableContent.AppendLine($@"
        [DataType(""{foreignTable.TableName}List"")]
        [Display(Name = ""{foreignTable.SingularName}"")]
        public virtual {foreignTable.TableName} {foreignTable.TableName} {{ get; protected set; }}");
		}

		tableContent.AppendLine($@"
        public class EntityConfiguration : IEntityTypeConfiguration<{table.TableName}>
        {{
		    public void Configure(EntityTypeBuilder<{table.TableName}> builder)
            {{
			    builder.HasKey(o => new {{ o.{string.Join(", o.", table.Indexes.Single(i => i.IsPrimaryKey).Columns.OrderBy(o => o.Position).ThenBy(o => o.ColumnName).Select(o => o.ColumnName))} }});
            }}
        }}
    }}
}}
");

		fileContents.Add(new FileContents
		{
			Filename = $"{table.TableName}.Generated.cs",
			Contents = tableContent.ToString(),
		});
	}

	if (!string.IsNullOrWhiteSpace(options.OutDir))
	{
		foreach (var fc in fileContents)
		{
			File.WriteAllText(Path.Combine(options.OutDir, fc.Filename), fc.Contents);
		}
		string.Join(", ", fileContents.Select(fc => fc.Filename)).Dump($"Scaffolded into {options.OutDir}");
	}
	else
	{
		fileContents.Dump();
	}
}

public class ScaffoldOptions
{
	public string Namespace { get; set; } = "Data";
	public string Context { get; set; } = "DataContext";
	public string OutDir { get; set; }
	public Predicate<DbTable> TableFilter { get; set; } = table => true;
}

public class FileContents
{
	public string Filename { get; set; }
	public string Contents { get; set; }
}

public class DbSchemaBuilder
{
	public DbSchemaBuilder(Dictionary<Type, string> builtInTypes, Dictionary<string, Type> clrTypes)
	{
		BuiltInTypes = builtInTypes;
		ClrTypes = clrTypes;
	}

	public Dictionary<Type, string> BuiltInTypes { get; set; }
	public Dictionary<string, Type> ClrTypes { get; set; }

	public async Task<DbSchema> CreateFromConnectionAsync(DbConnection connection)
	{
		var schema = new DbSchema();

		schema.Tables.AddRange(await connection.QueryAsync<DbTable>(
$@"SELECT
    CatalogName = TABLE_CATALOG,
    SchemaName = TABLE_SCHEMA,
    TableName = TABLE_NAME,
    TableType = CASE TABLE_TYPE
		WHEN 'VIEW' THEN {(int)DbTableType.View}
		WHEN 'BASE TABLE' THEN {(int)DbTableType.BaseTable}
		ELSE {(int)DbTableType.Unknown}
	END
from INFORMATION_SCHEMA.TABLES
"));

		foreach (var table in schema.Tables)
		{
			table.SingularName = table.TableName.Titleize();
			table.PluralName = table.SingularName.Pluralize();

			table.Columns.AddRange(await connection.QueryAsync<DbColumn>(
@"SELECT
    ColumnName = COLUMN_NAME,
	ColumnType = DATA_TYPE,
	ColumnDefault = COLUMN_DEFAULT,
	ColumnPosition = ORDINAL_POSITION,
	IsNullable = CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END,
	MaxLength = CHARACTER_MAXIMUM_LENGTH
from INFORMATION_SCHEMA.COLUMNS
where 
    @CatalogName = TABLE_CATALOG AND
    @SchemaName = TABLE_SCHEMA AND
    @TableName = TABLE_NAME
", table));
			foreach (var column in table.Columns)
			{
				column.ClrType = GetClrType(column);
				column.ClrTypeName = GetClrTypeName(column);
				column.SingularName = Regex.Replace(column.ColumnName.Titleize(), @" Id$", "");
				column.PluralName = column.SingularName.Pluralize();
			}

			var indexes = await connection.QueryAsync<Internals.DbIndex>(
@"SELECT
    IndexName = i.name,
    IndexType = i.type,
    IndexTypeDescription = i.type_desc,
    IsPrimaryKey = i.is_primary_key,
    IsUnique = i.is_unique,
    IsUniqueConstraint = i.is_unique_constraint,
    ColumnName = COL_NAME(ic.object_id,ic.column_id),
    Position = ic.key_ordinal
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.is_hypothetical = 0 AND
	OBJECT_SCHEMA_NAME(i.object_id) = @SchemaName AND
	OBJECT_NAME(i.object_id) = @TableName
", table);
			foreach (var group in indexes.GroupBy(r => new
			{
				r.IndexName,
				r.IndexType,
				r.IsPrimaryKey,
				r.IsUnique,
				r.IsUniqueConstraint
			}))
			{
				var index = new DbIndex
				{
					IndexName = group.Key.IndexName,
					IndexType = group.Key.IndexType,
					IsPrimaryKey = group.Key.IsPrimaryKey,
					IsUnique = group.Key.IsUnique,
					IsUniqueConstraint = group.Key.IsUniqueConstraint
				};
				index.Columns.AddRange(group.Select(r => new DbIndexColumn
				{
					ColumnName = r.ColumnName,
					Position = r.Position,
				}));
				table.Indexes.Add(index);
			}

			var tableConstraints = await connection.QueryAsync(
@"select
    ConstraintName = ccu.CONSTRAINT_NAME, 
    ConstraintType = tc.CONSTRAINT_TYPE,
    ColumnNames = STRING_AGG(c.COLUMN_NAME, ',')
from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu on 
        ccu.CONSTRAINT_CATALOG = tc.CONSTRAINT_CATALOG and
        ccu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA and
        ccu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        join INFORMATION_SCHEMA.COLUMNS c on 
            c.TABLE_CATALOG = ccu.CONSTRAINT_CATALOG and
            c.TABLE_SCHEMA = ccu.CONSTRAINT_SCHEMA and
            c.TABLE_NAME = ccu.TABLE_NAME and
            c.COLUMN_NAME = ccu.COLUMN_NAME
where 
    @CatalogName = c.TABLE_CATALOG AND
    @SchemaName = c.TABLE_SCHEMA AND
    @TableName = c.TABLE_NAME
group by c.TABLE_CATALOG, c.TABLE_SCHEMA, c.TABLE_NAME, ccu.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE
", table);
			foreach (var tableConstraint in tableConstraints)
			{
				var dbConstraint = new DbTableConstraint
				{
					ConstraintName = tableConstraint.ConstraintName,
					ConstraintType = (DbConstraintType)Enum.Parse(typeof(DbConstraintType), tableConstraint.ConstraintType.Replace(" ", ""), ignoreCase: true),
				};
				dbConstraint.ColumnNames.AddRange(tableConstraint.ColumnNames.Split(','));
				table.Constraints.Add(dbConstraint);
			}
		}

		var relationships = await connection.QueryAsync<Internals.TableColumnRelationship>(
@"SELECT 
    ConstraintCatalogName = tc.CONSTRAINT_CATALOG,
    ConstraintSchemaName = tc.CONSTRAINT_SCHEMA,
    ConstraintName = tc.CONSTRAINT_NAME,
    ForeignCatalogName = tc.TABLE_CATALOG,
    ForeignSchemaName = tc.TABLE_SCHEMA,
    ForeignTableName = tc.TABLE_NAME,
    ForeignColumnName = kcu.COLUMN_NAME,
    PrimaryCatalogName = tcPrimary.TABLE_CATALOG,
    PrimarySchemaName = tcPrimary.TABLE_SCHEMA,
    PrimaryTableName = tcPrimary.TABLE_NAME,
    PrimaryColumnName = kcuPrimary.COLUMN_NAME,
    DeleteRule = rc.DELETE_RULE,
    UpdateRule = rc.UPDATE_RULE
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
        ON tc.CONSTRAINT_CATALOG = kcu.CONSTRAINT_CATALOG
        AND tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA 
        AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME 
    JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc 
        ON tc.CONSTRAINT_CATALOG = rc.CONSTRAINT_CATALOG
        AND tc.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA 
        AND tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME 
    JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tcPrimary 
        ON rc.CONSTRAINT_CATALOG = tcPrimary.CONSTRAINT_CATALOG
        AND rc.UNIQUE_CONSTRAINT_SCHEMA = tcPrimary.CONSTRAINT_SCHEMA 
        AND rc.UNIQUE_CONSTRAINT_NAME = tcPrimary.CONSTRAINT_NAME 
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcuPrimary 
        ON tcPrimary.CONSTRAINT_CATALOG = kcuPrimary.CONSTRAINT_CATALOG
        AND tcPrimary.CONSTRAINT_SCHEMA = kcuPrimary.CONSTRAINT_SCHEMA 
        AND tcPrimary.CONSTRAINT_NAME = kcuPrimary.CONSTRAINT_NAME 
        AND kcu.ORDINAL_POSITION = kcuPrimary.ORDINAL_POSITION
");
		foreach (var rel in relationships.GroupBy(r => new DbRelationship
		{
			ConstraintCatalogName = r.ConstraintCatalogName,
			ConstraintSchemaName = r.ConstraintSchemaName,
			ConstraintName = r.ConstraintName,

			ForeignCatalogName = r.ForeignCatalogName,
			ForeignSchemaName = r.ForeignSchemaName,
			ForeignTableName = r.ForeignTableName,

			PrimaryCatalogName = r.PrimaryCatalogName,
			PrimarySchemaName = r.PrimarySchemaName,
			PrimaryTableName = r.PrimaryTableName,

			DeleteRule = (DbRelationshipRule)Enum.Parse(typeof(DbRelationshipRule), r.DeleteRule.Replace(" ", ""), ignoreCase: true),
			UpdateRule = (DbRelationshipRule)Enum.Parse(typeof(DbRelationshipRule), r.UpdateRule.Replace(" ", ""), ignoreCase: true),
		}))
		{
			rel.Key.Columns.AddRange(rel.Select(r => new DbRelationshipColumn
			{
				PrimaryColumnName = r.PrimaryColumnName,
				ForeignColumnName = r.ForeignColumnName
			}));
			schema.Relationships.Add(rel.Key);
		}

		return schema;
	}

	public Type GetClrType(DbColumn column)
	{
		if (!ClrTypes.TryGetValue(column.ColumnType, out Type type))
		{
			throw new NotImplementedException(column.ColumnType);
		}

		return column.IsNullable && type.IsValueType ?
			typeof(Nullable<>).MakeGenericType(type) :
			type;
	}

	public string GetClrTypeName(DbColumn column)
	{
		var nullable = column.ClrType.IsGenericType && column.ClrType.GetGenericTypeDefinition() == typeof(Nullable<>);
		Type type = nullable ?
			column.ClrType.GetGenericArguments()[0] :
			column.ClrType;

		if (!BuiltInTypes.TryGetValue(type, out string columnType))
		{
			columnType = type.Name;
		}

		return columnType + (nullable ? "?" : "");
	}

	internal static class Internals
	{
		internal class TableColumnRelationship
		{
			public string ConstraintCatalogName { get; set; }
			public string ConstraintSchemaName { get; set; }
			public string ConstraintName { get; set; }
			public string ForeignCatalogName { get; set; }
			public string ForeignSchemaName { get; set; }
			public string ForeignTableName { get; set; }
			public string ForeignColumnName { get; set; }
			public string PrimaryCatalogName { get; set; }
			public string PrimarySchemaName { get; set; }
			public string PrimaryTableName { get; set; }
			public string PrimaryColumnName { get; set; }
			public string DeleteRule { get; set; }
			public string UpdateRule { get; set; }
		}

		public class DbIndex
		{
			public string IndexName { get; set; }
			public DbIndexType IndexType { get; set; }
			public bool IsPrimaryKey { get; set; }
			public bool IsUnique { get; set; }
			public bool IsUniqueConstraint { get; set; }

			public string ColumnName { get; set; }
			public int Position { get; set; }
		}
	}
}

public class DbSchema
{
	public static class Defaults
	{
		/// <summary>
		/// Maps built-in types to the corresponding C# keyword.
		/// See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
		/// </summary>
		public static readonly Dictionary<Type, string> BuiltInTypes = new Dictionary<System.Type, string>
		{
			[typeof(bool)] = "bool",
			[typeof(byte)] = "byte",
			[typeof(byte[])] = "byte[]",
			[typeof(sbyte)] = "sbyte",
			[typeof(char)] = "char",
			[typeof(decimal)] = "decimal",
			[typeof(double)] = "double",
			[typeof(float)] = "float",
			[typeof(int)] = "int",
			[typeof(uint)] = "uint",
			[typeof(long)] = "long",
			[typeof(ulong)] = "ulong",
			[typeof(short)] = "short",
			[typeof(ushort)] = "ushort",

			[typeof(object)] = "object",
			[typeof(string)] = "string",
		};

		/// <summary>
		/// Maps DbType, SqlDbType, OleDbType, OdbcType strings to a CLR type.
		/// See: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types#specifying-parameter-data-types
		/// </summary>
		public static readonly Dictionary<string, Type> ClrTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase)
		{
			["Boolean"] = typeof(bool),
			["Bit"] = typeof(bool),

			["Byte"] = typeof(byte),
			["TinyInt"] = typeof(byte),
			["UnsignedTinyInt"] = typeof(byte),

			["Binary"] = typeof(byte[]),
			["VarBinary"] = typeof(byte[]),

			["Char"] = typeof(char),

			["Date"] = typeof(DateTime),
			["DBDate"] = typeof(DateTime),

			["datetime"] = typeof(DateTime),
			["DBTimeStamp"] = typeof(DateTime),

			["DateTimeOffset"] = typeof(DateTimeOffset),

			["Decimal"] = typeof(decimal),
			["Numeric"] = typeof(decimal),

			["Double"] = typeof(double),
			["Float"] = typeof(double),

			["Single"] = typeof(float),
			["Real"] = typeof(float),

			["Guid"] = typeof(Guid),
			["UniqueIdentifier"] = typeof(Guid),

			["Int16"] = typeof(short),
			["SmallInt"] = typeof(short),

			["Int"] = typeof(int),
			["Int32"] = typeof(int),

			["Int64"] = typeof(long),
			["BigInt"] = typeof(long),

			["Object"] = typeof(object),
			["Variant"] = typeof(object),

			["NChar"] = typeof(string),
			["WChar"] = typeof(string),

			["NVarChar"] = typeof(string),
			["String"] = typeof(string),
			["VarChar"] = typeof(string),
			["VarWChar"] = typeof(string),

			["DBTime"] = typeof(TimeSpan),
			["Time"] = typeof(TimeSpan),

			["UInt16"] = typeof(ushort),
			["UnsignedSmallInt"] = typeof(ushort),

			["UInt32"] = typeof(uint),
			["UnsignedInt"] = typeof(uint),

			["UInt64"] = typeof(ulong),
			["UnsignedBigInt"] = typeof(ulong),
		};
	}

	public List<DbTable> Tables { get; } = new List<DbTable>();
	public List<DbRelationship> Relationships { get; } = new List<DbRelationship>();
}

public class DbTable
{
	public string CatalogName { get; set; }
	public string SchemaName { get; set; }
	public string TableName { get; set; }
	public DbTableType TableType { get; set; }
	public string SingularName { get; set; }
	public string PluralName { get; set; }

	public List<DbColumn> Columns { get; } = new List<DbColumn>();
	public List<DbIndex> Indexes { get; } = new List<DbIndex>();
	public List<DbTableConstraint> Constraints { get; } = new List<DbTableConstraint>();
}

public enum DbTableType
{
	Unknown = 0,
	BaseTable = 1,
	View = 2,
}

public class DbColumn
{
	public string ColumnName { get; set; }
	public string ColumnType { get; set; }
	public string ColumnDefault { get; set; }
	public int ColumnPosition { get; set; }
	public bool IsNullable { get; set; }
	public int? MaxLength { get; set; }
	public Type ClrType { get; set; }
	public string ClrTypeName { get; set; }
	public string SingularName { get; set; }
	public string PluralName { get; set; }
}

public class DbTableConstraint
{
	public string ConstraintName { get; set; }
	public DbConstraintType ConstraintType { get; set; }
	public List<string> ColumnNames { get; } = new List<string>();
}

public enum DbConstraintType
{
	Unknown = 0,
	ForeignKey = 1,
	PrimaryKey = 2,
	Unique = 3,
}

public class DbIndex
{
	public string IndexName { get; set; }
	public DbIndexType IndexType { get; set; }
	public bool IsPrimaryKey { get; set; }
	public bool IsUnique { get; set; }
	public bool IsUniqueConstraint { get; set; }

	public List<DbIndexColumn> Columns { get; } = new List<DbIndexColumn>();
}

public class DbIndexColumn
{
	public string ColumnName { get; set; }
	public int Position { get; set; }
}

public enum DbIndexType
{
	Unknown = 0,
	Clustered = 1,
	NonClustered = 2,
}

public class DbRelationship
{
	public string ConstraintCatalogName { get; set; }
	public string ConstraintSchemaName { get; set; }
	public string ConstraintName { get; set; }

	public string ForeignCatalogName { get; set; }
	public string ForeignSchemaName { get; set; }
	public string ForeignTableName { get; set; }

	public string PrimaryCatalogName { get; set; }
	public string PrimarySchemaName { get; set; }
	public string PrimaryTableName { get; set; }

	public DbRelationshipRule DeleteRule { get; set; }
	public DbRelationshipRule UpdateRule { get; set; }

	public List<DbRelationshipColumn> Columns { get; } = new List<DbRelationshipColumn>();
}

public class DbRelationshipColumn
{
	public string ForeignColumnName { get; set; }
	public string PrimaryColumnName { get; set; }
}

public enum DbRelationshipRule
{
	NoAction = 0,
	Cascade = 1,
	SetNull = 2,
	SetDefault = 3,
}
