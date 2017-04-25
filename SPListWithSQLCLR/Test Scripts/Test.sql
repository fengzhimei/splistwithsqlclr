-- Examples for queries that exercise different SQL objects implemented by this assembly

-----------------------------------------------------------------------------------------
-- Stored procedure
-----------------------------------------------------------------------------------------
-- exec StoredProcedureName


-----------------------------------------------------------------------------------------
-- User defined function
-----------------------------------------------------------------------------------------
-- select dbo.FunctionName()


-----------------------------------------------------------------------------------------
-- User defined type
-----------------------------------------------------------------------------------------
-- CREATE TABLE test_table (col1 UserType)
-- go
--
-- INSERT INTO test_table VALUES (convert(uri, 'Instantiation String 1'))
-- INSERT INTO test_table VALUES (convert(uri, 'Instantiation String 2'))
-- INSERT INTO test_table VALUES (convert(uri, 'Instantiation String 3'))
--
-- select col1::method1() from test_table



-----------------------------------------------------------------------------------------
-- User defined type
-----------------------------------------------------------------------------------------
-- select dbo.AggregateName(Column1) from Table1


DECLARE	@return_value int,
		@resultMsg nvarchar(max)

EXEC	@return_value = [dbo].[InsertListItems]
		@tableName = N'VendorList',
		@webUrl = N'http://myworkbox',
		@listName = N'VendorList',
		@folderPath = NULL,
		@batchSize = 1,
		@resultMsg = @resultMsg OUTPUT

SELECT	@resultMsg as N'@resultMsg'

SELECT	'Return Value' = @return_value