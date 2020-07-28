# POCO Generator

POCO Generator is a .NET Core command-line tool, written in C#, to quickly create POCO objects from SQL Server tables and views.

Prior to running the program, connection string and namespace must be set in **appsettings.json** file
```sh
{
  "connectionString": "Password=<PASSWORD>;Persist Security Info=True;User ID=<USERNAME>;Initial Catalog=<DATABASE>;Data Source=<SQL_SERVER_INSTANCE>",
  "namespace":  "pocoGenerator_classes"
}
```

The tool can be run without parameters, in which case it will process every table and view in the given database, like that:
```sh
c:\> pocogenerator.exe
```
Or by specifying a particular table, in which case it will create a single .cs file for that table (or view)
```sh
c:\> pocogenerator.exe MyTable
```

POCO Generator will create a folder named **pocoGenerator** on the current user's desktop, where it will save the created .cs files.