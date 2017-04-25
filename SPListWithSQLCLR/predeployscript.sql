IF EXISTS (SELECT [name] FROM sys.assemblies WHERE [name] = N'SPListWithSQLCLR.XmlSerializers') 
DROP ASSEMBLY [SPListWithSQLCLR.XmlSerializers] with NO DEPENDENTS;