# Example: Async Custom API with Background Processing

This example demonstrates creating an async Custom API that processes large data operations in the background.

---

## Scenario

**Use Case**: Generate a comprehensive analytics report for an account that includes:
- All opportunities and their revenue
- All cases and resolution metrics
- Contact engagement scores
- Historical trends

This operation can take 30-60 seconds for large accounts, so it runs asynchronously.

---

## Custom API Definition

**Unique Name**: `contoso_GenerateAccountReport`  
**Display Name**: Generate Account Report  
**Description**: Generates comprehensive analytics report for an account (async)  
**Binding Type**: Entity (1)  
**Bound Entity**: account  
**Is Function**: false (Action)  
**Is Private**: false (Public)

---

## Request Parameters

| Unique Name | Display Name | Type | Optional | Description |
|------------|--------------|------|----------|-------------|
| `Target` | Target | EntityReference (5) | No | The account to generate report for (auto-provided for bound) |
| `IncludeHistorical` | Include Historical | Boolean (0) | Yes | Include data from past 5 years (default: false) |
| `ReportFormat` | Report Format | String (10) | Yes | Output format: JSON, XML, or CSV (default: JSON) |

---

## Response Properties

| Unique Name | Display Name | Type | Description |
|------------|--------------|------|-------------|
| `JobId` | Job Id | Guid (12) | Background job identifier |
| `EstimatedCompletionTime` | Estimated Completion | DateTime (1) | When the report should be ready |
| `StatusUrl` | Status URL | String (10) | URL to check job status |

---

## Implementation Pattern

### Architecture

1. **Custom API**: Initiates the job and returns immediately
2. **Background Job**: Processes data asynchronously
3. **Status API**: Separate Custom API to check job status
4. **Notification**: Optional webhook/email when complete

### 1. Main Custom API Implementation

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Contoso.CustomApis
{
    /// <summary>
    /// Initiates async generation of account analytics report.
    /// Returns immediately with job ID for status tracking.
    /// </summary>
    public class GenerateAccountReportApi : IPlugin
    {
        private const string REPORT_JOB_ENTITY = "contoso_reportjob";

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("GenerateAccountReportApi: Starting execution");

            try
            {
                // Extract parameters
                var accountRef = (EntityReference)context.InputParameters["Target"];
                bool includeHistorical = context.InputParameters.Contains("IncludeHistorical")
                    ? (bool)context.InputParameters["IncludeHistorical"]
                    : false;
                string reportFormat = context.InputParameters.Contains("ReportFormat")
                    ? (string)context.InputParameters["ReportFormat"]
                    : "JSON";

                tracingService.Trace($"Account: {accountRef.Id}, Historical: {includeHistorical}, Format: {reportFormat}");

                // Validate account exists
                ValidateAccount(service, accountRef, tracingService);

                // Create job record to track async processing
                var jobId = CreateJobRecord(service, accountRef, includeHistorical, reportFormat, tracingService);

                // Estimate completion time (based on historical data size)
                var estimatedCompletion = EstimateCompletionTime(service, accountRef, includeHistorical);

                // Schedule async processing (using workflow, async plugin, or Azure Function)
                ScheduleAsyncProcessing(service, jobId, tracingService);

                // Return job tracking info
                context.OutputParameters["JobId"] = jobId;
                context.OutputParameters["EstimatedCompletionTime"] = estimatedCompletion;
                context.OutputParameters["StatusUrl"] = $"/api/data/v9.2/contoso_GetReportStatus(JobId={jobId})";

                tracingService.Trace($"Job created: {jobId}");
                tracingService.Trace("GenerateAccountReportApi: Completed successfully");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Failed to initiate report generation: {ex.Message}", ex);
            }
        }

        private void ValidateAccount(IOrganizationService service, EntityReference accountRef, ITracingService tracingService)
        {
            try
            {
                var account = service.Retrieve("account", accountRef.Id, new ColumnSet("name"));
                tracingService.Trace($"Account validated: {account.GetAttributeValue<string>("name")}");
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException($"Account {accountRef.Id} not found or inaccessible.", ex);
            }
        }

        private Guid CreateJobRecord(
            IOrganizationService service,
            EntityReference accountRef,
            bool includeHistorical,
            string reportFormat,
            ITracingService tracingService)
        {
            var job = new Entity(REPORT_JOB_ENTITY)
            {
                ["contoso_name"] = $"Account Report - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                ["contoso_accountid"] = accountRef,
                ["contoso_includehistorical"] = includeHistorical,
                ["contoso_reportformat"] = reportFormat,
                ["contoso_status"] = new OptionSetValue(1), // 1 = Queued
                ["contoso_createdon"] = DateTime.UtcNow,
                ["contoso_progress"] = 0
            };

            var jobId = service.Create(job);
            tracingService.Trace($"Job record created: {jobId}");
            return jobId;
        }

        private DateTime EstimateCompletionTime(
            IOrganizationService service,
            EntityReference accountRef,
            bool includeHistorical)
        {
            // Query related records to estimate processing time
            int opportunityCount = CountRelatedRecords(service, "opportunity", "parentaccountid", accountRef.Id);
            int caseCount = CountRelatedRecords(service, "incident", "customerid", accountRef.Id);
            int contactCount = CountRelatedRecords(service, "contact", "parentcustomerid", accountRef.Id);

            int totalRecords = opportunityCount + caseCount + contactCount;
            
            // Base: 10 seconds + 0.5 seconds per 100 records
            // Historical adds 20 seconds
            int estimatedSeconds = 10 + (totalRecords / 100) + (includeHistorical ? 20 : 0);

            return DateTime.UtcNow.AddSeconds(estimatedSeconds);
        }

        private int CountRelatedRecords(IOrganizationService service, string entityName, string lookupField, Guid accountId)
        {
            var query = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(lookupField, ConditionOperator.Equal, accountId)
                    }
                }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Count;
        }

        private void ScheduleAsyncProcessing(IOrganizationService service, Guid jobId, ITracingService tracingService)
        {
            // Option 1: Trigger an async plugin step (registered on custom entity update)
            var job = new Entity(REPORT_JOB_ENTITY, jobId)
            {
                ["contoso_status"] = new OptionSetValue(2) // 2 = Processing
            };
            service.Update(job);

            // Option 2: Create a workflow instance
            // var workflowRequest = new ExecuteWorkflowRequest
            // {
            //     WorkflowId = REPORT_GENERATION_WORKFLOW_ID,
            //     EntityId = jobId
            // };
            // service.Execute(workflowRequest);

            // Option 3: Call Azure Function via HTTP (best for long-running)
            // var httpRequest = CreateHttpRequest(jobId);
            // HttpClient.PostAsync(AZURE_FUNCTION_URL, httpRequest);

            tracingService.Trace("Async processing scheduled");
        }
    }
}
```

### 2. Async Processing Plugin

This plugin runs asynchronously to do the actual report generation:

```csharp
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Text.Json;

namespace Contoso.CustomApis
{
    /// <summary>
    /// Async plugin that processes report generation in the background.
    /// Triggered when report job status changes to "Processing".
    /// </summary>
    /// <remarks>
    /// Register this on Update of contoso_reportjob entity, async mode.
    /// </remarks>
    public class ProcessReportGenerationPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("ProcessReportGenerationPlugin: Starting");

            try
            {
                // Get the job record
                var jobId = context.PrimaryEntityId;
                var job = service.Retrieve("contoso_reportjob", jobId, 
                    new ColumnSet("contoso_accountid", "contoso_includehistorical", "contoso_reportformat"));

                var accountRef = job.GetAttributeValue<EntityReference>("contoso_accountid");
                bool includeHistorical = job.GetAttributeValue<bool>("contoso_includehistorical");
                string reportFormat = job.GetAttributeValue<string>("contoso_reportformat");

                tracingService.Trace($"Processing report for account: {accountRef.Id}");

                // Generate the report
                var reportData = GenerateReport(service, accountRef, includeHistorical, tracingService);
                
                // Format the output
                string formattedReport = FormatReport(reportData, reportFormat);

                // Store the result
                StoreReport(service, jobId, formattedReport, tracingService);

                // Update job status to completed
                UpdateJobStatus(service, jobId, 3, 100, null, tracingService); // 3 = Completed

                tracingService.Trace("Report generation completed successfully");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                
                // Update job status to failed
                UpdateJobStatus(service, context.PrimaryEntityId, 4, 0, ex.Message, tracingService); // 4 = Failed
                
                // Don't throw - we've already logged the error in the job record
            }
        }

        private ReportData GenerateReport(
            IOrganizationService service,
            EntityReference accountRef,
            bool includeHistorical,
            ITracingService tracingService)
        {
            var report = new ReportData
            {
                AccountId = accountRef.Id,
                GeneratedOn = DateTime.UtcNow
            };

            // Get opportunities
            tracingService.Trace("Retrieving opportunities...");
            report.Opportunities = GetOpportunities(service, accountRef.Id, includeHistorical);
            UpdateJobProgress(service, accountRef.Id, 25);

            // Get cases
            tracingService.Trace("Retrieving cases...");
            report.Cases = GetCases(service, accountRef.Id, includeHistorical);
            UpdateJobProgress(service, accountRef.Id, 50);

            // Get contacts
            tracingService.Trace("Retrieving contacts...");
            report.Contacts = GetContacts(service, accountRef.Id);
            UpdateJobProgress(service, accountRef.Id, 75);

            // Calculate metrics
            tracingService.Trace("Calculating metrics...");
            report.Metrics = CalculateMetrics(report);
            UpdateJobProgress(service, accountRef.Id, 90);

            return report;
        }

        private OpportunityData[] GetOpportunities(IOrganizationService service, Guid accountId, bool includeHistorical)
        {
            var query = new QueryExpression("opportunity")
            {
                ColumnSet = new ColumnSet("name", "estimatedvalue", "closeprobability", "actualclosedate", "statecode"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("parentaccountid", ConditionOperator.Equal, accountId)
                    }
                }
            };

            if (!includeHistorical)
            {
                // Only last 12 months
                query.Criteria.AddCondition("createdon", ConditionOperator.LastXMonths, 12);
            }

            var results = service.RetrieveMultiple(query);
            
            return results.Entities.Select(e => new OpportunityData
            {
                Name = e.GetAttributeValue<string>("name"),
                EstimatedValue = e.GetAttributeValue<Money>("estimatedvalue")?.Value ?? 0,
                CloseProbability = e.GetAttributeValue<int>("closeprobability"),
                Status = e.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0
            }).ToArray();
        }

        private CaseData[] GetCases(IOrganizationService service, Guid accountId, bool includeHistorical)
        {
            // Similar to GetOpportunities
            // Implementation omitted for brevity
            return new CaseData[0];
        }

        private ContactData[] GetContacts(IOrganizationService service, Guid accountId)
        {
            // Similar to GetOpportunities
            // Implementation omitted for brevity
            return new ContactData[0];
        }

        private MetricsData CalculateMetrics(ReportData report)
        {
            return new MetricsData
            {
                TotalOpportunityValue = report.Opportunities.Sum(o => o.EstimatedValue),
                AverageCloseProbability = report.Opportunities.Average(o => o.CloseProbability),
                TotalCases = report.Cases.Length,
                TotalContacts = report.Contacts.Length
            };
        }

        private string FormatReport(ReportData data, string format)
        {
            switch (format.ToUpper())
            {
                case "JSON":
                    return JsonSerializer.Serialize(data);
                case "XML":
                    // XML serialization implementation
                    return "<report><!-- XML content --></report>";
                case "CSV":
                    // CSV formatting implementation
                    return "field1,field2,field3\nvalue1,value2,value3";
                default:
                    return JsonSerializer.Serialize(data);
            }
        }

        private void StoreReport(IOrganizationService service, Guid jobId, string reportContent, ITracingService tracingService)
        {
            // Store as note/annotation
            var annotation = new Entity("annotation")
            {
                ["objectid"] = new EntityReference("contoso_reportjob", jobId),
                ["objecttypecode"] = "contoso_reportjob",
                ["subject"] = "Generated Report",
                ["notetext"] = reportContent.Length > 5000 ? reportContent.Substring(0, 5000) : reportContent,
                ["filename"] = $"report_{jobId}.json"
            };

            service.Create(annotation);
            tracingService.Trace("Report stored as annotation");
        }

        private void UpdateJobProgress(IOrganizationService service, Guid jobId, int progressPercent)
        {
            var job = new Entity("contoso_reportjob", jobId)
            {
                ["contoso_progress"] = progressPercent
            };
            service.Update(job);
        }

        private void UpdateJobStatus(
            IOrganizationService service,
            Guid jobId,
            int statusCode,
            int progress,
            string errorMessage,
            ITracingService tracingService)
        {
            var job = new Entity("contoso_reportjob", jobId)
            {
                ["contoso_status"] = new OptionSetValue(statusCode),
                ["contoso_progress"] = progress,
                ["contoso_completedon"] = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(errorMessage))
            {
                job["contoso_errormessage"] = errorMessage;
            }

            service.Update(job);
            tracingService.Trace($"Job status updated to {statusCode}");
        }

        // Data classes
        private class ReportData
        {
            public Guid AccountId { get; set; }
            public DateTime GeneratedOn { get; set; }
            public OpportunityData[] Opportunities { get; set; }
            public CaseData[] Cases { get; set; }
            public ContactData[] Contacts { get; set; }
            public MetricsData Metrics { get; set; }
        }

        private class OpportunityData
        {
            public string Name { get; set; }
            public decimal EstimatedValue { get; set; }
            public int CloseProbability { get; set; }
            public int Status { get; set; }
        }

        private class CaseData { }
        private class ContactData { }
        
        private class MetricsData
        {
            public decimal TotalOpportunityValue { get; set; }
            public double AverageCloseProbability { get; set; }
            public int TotalCases { get; set; }
            public int TotalContacts { get; set; }
        }
    }
}
```

### 3. Status Check Custom API

Create a separate Custom API to check job status:

```csharp
namespace Contoso.CustomApis
{
    /// <summary>
    /// Custom API to check status of report generation job.
    /// </summary>
    public class GetReportStatusApi : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                var jobId = (Guid)context.InputParameters["JobId"];
                
                var job = service.Retrieve("contoso_reportjob", jobId,
                    new ColumnSet("contoso_status", "contoso_progress", "contoso_errormessage", "contoso_completedon"));

                var status = job.GetAttributeValue<OptionSetValue>("contoso_status")?.Value ?? 0;
                var progress = job.GetAttributeValue<int>("contoso_progress");
                var errorMessage = job.GetAttributeValue<string>("contoso_errormessage");
                var completedOn = job.Contains("contoso_completedon") 
                    ? job.GetAttributeValue<DateTime>("contoso_completedon") 
                    : (DateTime?)null;

                // Set outputs
                context.OutputParameters["Status"] = status; // 1=Queued, 2=Processing, 3=Completed, 4=Failed
                context.OutputParameters["Progress"] = progress;
                context.OutputParameters["IsComplete"] = status == 3 || status == 4;
                context.OutputParameters["ErrorMessage"] = errorMessage ?? string.Empty;
                
                if (status == 3 && completedOn.HasValue)
                {
                    context.OutputParameters["CompletedOn"] = completedOn.Value;
                    
                    // Get report URL if completed
                    context.OutputParameters["ReportUrl"] = $"/api/data/v9.2/contoso_GetReport(JobId={jobId})";
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}");
                throw new InvalidPluginExecutionException($"Failed to get report status: {ex.Message}", ex);
            }
        }
    }
}
```

---

## Usage Examples

### From JavaScript with Polling

```javascript
// Start report generation
function generateAccountReport(accountId) {
    var request = {
        entity: { 
            entityType: "account", 
            id: accountId 
        },
        IncludeHistorical: false,
        ReportFormat: "JSON",
        
        getMetadata: function() {
            return {
                boundParameter: "entity",
                parameterTypes: {
                    "entity": { typeName: "mscrm.account", structuralProperty: 5 },
                    "IncludeHistorical": { typeName: "Edm.Boolean", structuralProperty: 1 },
                    "ReportFormat": { typeName: "Edm.String", structuralProperty: 1 }
                },
                operationType: 0,
                operationName: "contoso_GenerateAccountReport"
            };
        }
    };

    Xrm.Utility.showProgressIndicator("Starting report generation...");

    Xrm.WebApi.online.execute(request).then(
        function(response) {
            return response.json();
        }
    ).then(function(result) {
        var jobId = result.JobId;
        var estimatedTime = new Date(result.EstimatedCompletionTime);
        
        Xrm.Utility.closeProgressIndicator();
        Xrm.Utility.alertDialog(
            "Report generation started. Check back in " + 
            Math.round((estimatedTime - new Date()) / 1000) + " seconds."
        );
        
        // Poll for completion
        pollReportStatus(jobId);
    }).catch(function(error) {
        Xrm.Utility.closeProgressIndicator();
        console.error("Error:", error);
    });
}

// Poll status every 5 seconds
function pollReportStatus(jobId) {
    var checkStatus = function() {
        var statusRequest = {
            JobId: jobId,
            
            getMetadata: function() {
                return {
                    boundParameter: null,
                    parameterTypes: {
                        "JobId": { typeName: "Edm.Guid", structuralProperty: 1 }
                    },
                    operationType: 0,
                    operationName: "contoso_GetReportStatus"
                };
            }
        };

        Xrm.WebApi.online.execute(statusRequest).then(
            function(response) {
                return response.json();
            }
        ).then(function(result) {
            var isComplete = result.IsComplete;
            var progress = result.Progress;
            var status = result.Status;

            if (!isComplete) {
                // Update progress and check again
                Xrm.Utility.showProgressIndicator("Generating report... " + progress + "%");
                setTimeout(checkStatus, 5000); // Check again in 5 seconds
            } else {
                Xrm.Utility.closeProgressIndicator();
                
                if (status === 3) {
                    // Completed successfully
                    Xrm.Utility.alertDialog("Report completed! Download from: " + result.ReportUrl);
                } else {
                    // Failed
                    Xrm.Utility.alertDialog("Report failed: " + result.ErrorMessage);
                }
            }
        });
    };

    checkStatus();
}
```

### From Power Automate with Wait

1. **Add action**: Perform an unbound action → `contoso_GenerateAccountReport`
2. **Get outputs**: JobId, EstimatedCompletionTime
3. **Add delay**: Wait until EstimatedCompletionTime
4. **Add action**: Perform an unbound action → `contoso_GetReportStatus`
5. **Add condition**: If IsComplete = true
6. **Add action**: Send email with report link

---

## Key Patterns

✅ **Immediate return**: Don't make callers wait  
✅ **Job tracking**: Create records to track progress  
✅ **Status API**: Provide way to check progress  
✅ **Error handling**: Store errors in job record  
✅ **Progress updates**: Update progress percentage  
✅ **Async processing**: Use async plugins or Azure Functions  

This pattern works well for:
- Large data exports
- Complex calculations
- External API calls
- Report generation
- Batch operations
