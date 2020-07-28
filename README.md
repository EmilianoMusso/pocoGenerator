# POCO Generator

POCO Generator is a .NET Core command-line tool, written in C#, to quickly create POCO objects from SQL Server tables and views, with class definition 
for stored procedures and function too (those last two are in development, for now the program will only create their declaration).
It aims to prepare classes and DataModel for Entity Framework, according to Code-First paradigm.

Prior to running the program, connection string, namespace and data model name must be set in **appsettings.json** file
```sh
{
  "connectionString": "Password=<PASSWORD>;Persist Security Info=True;User ID=<USERNAME>;Initial Catalog=<DATABASE>;Data Source=<SQL_SERVER_INSTANCE>",
  "namespace":  "pocoGenerator_classes",
  "datamodelname":  "pocoDataModel"
}
```

The tool can be run without parameters, in which case it will process every table, view, stored procedure and function in the given database, like that:
```sh
c:\> pocogenerator.exe
```
Or by specifying a particular table, in which case it will create a single .cs file for that table, view, stored procedure or function
```sh
c:\> pocogenerator.exe MyTable
```

POCO Generator will create a folder named **pocoGenerator** on the current user's desktop, where it will save the created .cs files.
DataModel is always created, and it contains for now only tables and views.
DataAnnotations will be used into classes to mark fields which compose the primary key, and for identity / computed columns