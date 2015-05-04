
#region namespaces

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI; // TaskDialog 

#endregion

namespace DynamoMeshBar
{
   // IDisposable i essential to properly handle (i.e. close and flush) open files.
   // IDataConversionMonitor is, of course, why we are here in the first place.
   class ConversionLog: IDataConversionMonitor, IDisposable
   {
      #region // data , properties, constructor, destructor
      StreamWriter m_logStream;  // output file
      Dictionary<string, int> m_convertedEntityList;
      Dictionary<string, int> m_skippedEntityList;
      Dictionary<DataExchangeMessageId, string> m_messageList; // this converts from message ids to text. may be localized in the future.

      // properties
      public string Name                                 { get; set; }  // name of this logger object. We can have multiple loggers!
      public DataExchangeMessageVerbosity Verbosity { get; set; }
      public DataExchangeMessageSeverity CancelCondition { get; set; }
      bool CancelRequested { get; set; }  // UI requested a cancellation
      bool FatalError { get; set; } // Fatal error encountered
      int ErrorCount { get; set; } // Errors encountered
      int WarningCount { get; set; } // Warnings encountered

      // constructor, init functions
      public ConversionLog(string logFileName)
      {
         m_convertedEntityList = new Dictionary<string, int>();
         m_skippedEntityList = new Dictionary<string,int>();

         // MSDN promises auto-init capability for properties in C# 6. We are not quite there yet, so init properties here.
         Name = "Conversion Log"; 
         Verbosity = DataExchangeMessageVerbosity.Verbose;
         CancelCondition = DataExchangeMessageSeverity.FatalError;
         CancelRequested = false;
         FatalError = false;
         ErrorCount = 0;
         WarningCount = 0;

         InitMessageList();

         try
         {
            m_logStream = new StreamWriter(logFileName);
         }
         catch (System.UnauthorizedAccessException)
         {

         }
         catch (System.ArgumentException)   // Also gets ArgumentNullException
         {

         }
         catch (System.IO.DirectoryNotFoundException)
         {

         }
         catch (System.IO.PathTooLongException)
         {

         }
         catch (System.IO.IOException)
         {

         }
         catch (System.Security.SecurityException)
         {

         }

         if (null == m_logStream)
         {
           // TaskDialog.Show(Properties.Resource.TaskDialogMessageCannotCreateLogFile, logFileName);
         }
         
      } // end constructor

      private void InitMessageList()
      {
         m_messageList = new Dictionary<DataExchangeMessageId, string>();

         //m_messageList.Add(DataExchangeMessageId.GenericError, Properties.Resource.Error_UnspecifiedError);
         //m_messageList.Add(DataExchangeMessageId.InvalidDataSet, Properties.Resource.Error_InvalidDataSet);
         //m_messageList.Add(DataExchangeMessageId.InvalidSourceObject, Properties.Resource.Error_InvalidObject);
         //m_messageList.Add(DataExchangeMessageId.ObjectCreated,  Properties.Resource.Error_ObjectCreated);
         //m_messageList.Add(DataExchangeMessageId.ObjectNotConverted, Properties.Resource.Error_ObjectNotConverted);
         //m_messageList.Add(DataExchangeMessageId.ObjectNotSupported, Properties.Resource.Error_ObjectNotSupported);
         //m_messageList.Add(DataExchangeMessageId.UnexpectedResult, Properties.Resource.Error_UnexpectedResult);
         //m_messageList.Add(DataExchangeMessageId.InvalidRenderingStyle, Properties.Resource.Error_InvalidRenderingStyle);

         // these two shouldn't require text.
         m_messageList.Add(DataExchangeMessageId.UnitOfProgressCompleted, "");
         m_messageList.Add(DataExchangeMessageId.None, "");
      }


      #endregion
      #region      // virtuals for IDataConversionMonitor, IDisposable

      public virtual void Dispose()
      {
         WriteStats();
         m_logStream.Close();
      }
      public virtual DataExchangeMessageVerbosity GetVerbosity() { return Verbosity; }

      public virtual bool ProcessMessage(DataExchangeMessageId messageId, DataExchangeMessageSeverity messageSeverity, System.Collections.Generic.IList<string> entityIds)
      {
         // First update counters
         switch (messageSeverity)
         {
            case DataExchangeMessageSeverity.FatalError:
               {
                  //WriteLine(Properties.Resource.ErrorMessage_OnFatalError);
                  FatalError = true;
                  break; // we always abort on a fatal error
               }
            case DataExchangeMessageSeverity.Error:
               ErrorCount += 1;
               break;
            case DataExchangeMessageSeverity.Warning:
               WarningCount += 1;
               break;
            case DataExchangeMessageSeverity.Info:
               break;
            default:
               break; // should have a debug message
         }


         // Next pick up messages that need special treatment

         switch (messageId)
         {
            case DataExchangeMessageId.ObjectCreated:
               // log created objects. The entityIds list is expected to contain object types as strings.
               foreach (var name in entityIds)
               {
                  if (! m_convertedEntityList.ContainsKey(name))
                  {
                     m_convertedEntityList.Add(name, 1); // first time we see this type
                  }
                  else
                  {
                     m_convertedEntityList[name] += 1;
                  }
               }
               return true; // logged another entity converted, continue
            //case DataExchangeMessageId.ObjectNotSupported:
            //// log not yet supported objects that were skipped during conversion. The entityIds list is expected to contain object types as strings.
            //   foreach (var name in entityIds)
            //   {
            //      if (!m_skippedEntityList.ContainsKey(name))
            //      {
            //         m_skippedEntityList.Add(name, 1); // first time we see this type
            //      }
            //      else
            //      {
            //         m_skippedEntityList[name] += 1;
            //      }
            //   }
            //   return true; // logged another entity converted, continue

            case DataExchangeMessageId.UnitOfProgressCompleted:
               return !CancelRequested; // CancelRequested == true means UI requested a cancellation. Not enabled in UI yet
         }

         // report other messages


         m_logStream.WriteLine();
         WriteLine(MessageIdToText(messageId));

         //WriteReportedEntities(Properties.Resource.LogMessage_EntitiesProcessed , entityIds);

         if (CancelCondition <= messageSeverity) // Cancel condition is less or equal to current message severity: terminate
         {
            //WriteReportedEntities(Properties.Resource.LogMessage_ConversionCancelled, entityIds);
            return false;
         }


         // TODO: design and implement a cancel condition. Possibly reuse the severity or verbosity enum: cancel at error, at warning, etc.
         return true;
      }
      #endregion

      #region // local helper functions

      private void WriteReportedEntities(string header, System.Collections.Generic.IList<string> entityIds)
      {
         
         if (entityIds.Count() != 0)
         {
            WriteLine(header);
            string reportedEntities = "";
            foreach (string entityId in entityIds)
            {
               reportedEntities += entityId;
               reportedEntities += " ";
            }
            WriteLine(reportedEntities);
         }
      }

      private void WriteStats()
      {
         if (DataExchangeMessageVerbosity.Verbose == Verbosity)
         {

            m_logStream.WriteLine();
            //WriteLine(Properties.Resource.LogMessage_ConversionStats);
            m_logStream.WriteLine();
            if (!FatalError)
            {
              // WriteLine(Properties.Resource.LogMessage_ConversionStatsConverted);
               foreach (var entityCount in m_convertedEntityList)
               {
                  WriteLine(entityCount.Key + ": " + entityCount.Value.ToString());
               }

               if (0 != m_skippedEntityList.Count)
               {
                  m_logStream.WriteLine();
                  //WriteLine(Properties.Resource.LogMessage_ConversionStatsSkipped);
                  foreach (var entityCount in m_skippedEntityList)
                  {
                     WriteLine(entityCount.Key + ": " + entityCount.Value.ToString());
                  }
               }
               m_logStream.WriteLine();
               //WriteLine(Properties.Resource.LogMessage_EndConversionStats); // not sure we need that in the log file
            }
         }

         // Report success/failure
         m_logStream.WriteLine();
         if (FatalError || 0 == m_convertedEntityList.Count)
         {
            //WriteLine(Properties.Resource.LogMessage_ConversionFailed);
         }
         else
         {
            //WriteLine(Properties.Resource.LogMessage_ConversionSuccessful);
         }
         m_logStream.WriteLine();

         // report errors if any
         if ( 0 != ErrorCount)
         {
            //WriteLine(Properties.Resource.LogMessage_Errors + " " + ErrorCount.ToString());
            m_logStream.WriteLine();
         }

         // report warnings if any
         if (0 != WarningCount)
         {
            //WriteLine(Properties.Resource.LogMessage_Warnings + " " + WarningCount.ToString());
            m_logStream.WriteLine();
         }
         

      }

      private void WriteLine(string line)
      {
         // log stream exists, and the line is not empty
         if (null != m_logStream && "" != line)
         {
            m_logStream.Write(line);
            m_logStream.WriteLine();
            m_logStream.Flush(); // to make sure the log file is up to date.
         }
      }

      private string MessageIdToText(DataExchangeMessageId id)
      {
         string messageText = "";

         try
         {
            messageText = m_messageList[id];
         }
         catch (System.Collections.Generic.KeyNotFoundException)
         {
            TaskDialog.Show("Conversion log", "Message id not found"); // debug message - shouldn't be visible to users. probably shouldn't be in TaskDialog. 
         }
         catch (System.ArgumentNullException)
         {
            TaskDialog.Show("ConversionLog", "Invalid message id");  // debug message - shouldn't be visible to users. probably shouldn't be in TaskDialog. 
         }

         return messageText;
      }


      #endregion
   }
}
