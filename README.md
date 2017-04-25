# Manipulating SharePoint List Data with SQLCLR

## Project Description

Basically there are two SQLCLR Stored Procedures contained in the solution - InsertListItems and UpdateListItems. As the name implies, InsertListItems can be used to create Sharepoint list items base on a table/view data and UpdataListItems does the updating.

### Logic for InsertListItems --

1. Read all records from the SQL table/view name
2. Perform the following validation
    1. Check to make sure that all the field name from the SQL table/view exists in SharePoint list
    2. Check to make sure field types are consistent
    3. Check to make sure required fields (in the SP List) are included in the SQL table/view
3. Upon successful validation, perform bulk insert to insert all the records to SharePoint list

### Logic for UpdateListItems --

1. Read all records from the SQL table/view name
2. Perform the following validation
    1. Check to make sure that all the field name from the SQL table/view exists in SharePoint list
    2. Check to make sure field types are consistent
3. Upon successful validation, perform update to SharePoint List
