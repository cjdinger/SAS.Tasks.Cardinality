# SAS.Tasks.Cardinality
***
SAS Enterprise Guide custom task that calculates the number of distinct values for each variable in a data set. This version is implemented in C#.


Written by [Chris Hemedinger](http://blogs.sas.com/content/sasdummy) as an example.  It's one of a series of examples that accompany
_Custom Tasks for SAS Enterprise Guide using Microsoft .NET_ 
by [Chris Hemedinger](http://support.sas.com/hemedinger).

You can read more about how to use the task in this blog post:
[A custom task to check your data cardinality](http://blogs.sas.com/content/sasdummy/2013/10/18/a-custom-task-to-check-your-data-cardinality/)

## About this example
This sample demonstrates a couple of useful techniques, including:
- Submit a block of SAS code to the SAS session while your task UI is showing.  The code runs asynchronously.  Threading techniques are used to 
marshal the results back to the UI thread.
- Show a "progress" form that allows you to cancel the asynchronous submit step, if needed.
- Uses the SAS OLE DB data provider to read the contents of a SAS data set and populate the task UI with information.
- Use the SAS.Tasks.Toolkit helper functions to add niceties to the task, such as a standard SAS program code header (SAS.Tasks.Toolkit.Helpers.UtilityFunctions.BuildSasTaskCodeHeader).
