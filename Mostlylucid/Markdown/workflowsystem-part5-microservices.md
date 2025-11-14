# Building a Workflow System with HTMX and ASP.NET Core - Part 5: Breaking the Monolith
<!--category-- ASP.NET, Microservices, Workflow, Architecture -->
<datetime class="hidden">2025-01-16T12:00</datetime>

## Introduction

In the [previous parts](/blog/workflowsystem-part1-introduction) of this series, we built a complete workflow system - from the core engine to the visual editor to Hangfire automation. But we've been building a monolith. Every node type, every capability, lives in the same codebase. This works great for simple workflows, but what happens when you need:

- Email sending with complex templating
- Webhook handling with retry logic and signature verification
- API calls that might take minutes to complete
- Database operations that need connection pooling
- AI integrations with GPU requirements
- Custom business logic that different teams own

The answer? **Microservices-based workflow nodes.**

Instead of cramming all functionality into one application, we can build an **orchestration engine** where each complex operation is handled by a specialist microservice. The workflow becomes a choreographer, coordinating these services through JSON definitions.

[TOC]

## The Problem with Monolithic Workflow Nodes

Let's look at what we currently have. Our workflow engine executes nodes like this:

```csharp
public async Task<NodeExecutionResult> ExecuteNodeAsync(
    WorkflowNode node,
    Dictionary<string, object> context)
{
    return node.Type switch
    {
        "HttpRequest" => await ExecuteHttpRequestNode(node, context),
        "Transform" => await ExecuteTransformNode(node, context),
        "Condition" => await ExecuteConditionNode(node, context),
        "Log" => await ExecuteLogNode(node, context),
        "SendEmail" => await ExecuteSendEmailNode(node, context),
        "CallWebhook" => await ExecuteWebhookNode(node, context),
        // ... more and more types
        _ => throw new NotSupportedException($"Node type {node.Type} not supported")
    };
}
```

**Problems:**
1. **Scalability**: Email sending blocks the main thread
2. **Deployment**: To update email logic, redeploy the entire workflow engine
3. **Ownership**: One team can't update their node logic without affecting everyone
4. **Resource usage**: AI nodes need GPUs, but the workflow engine doesn't
5. **Language lock-in**: Everything must be C#
6. **Testing**: Testing email nodes requires running the whole workflow engine

## The Microservices Architecture

Here's the new vision:

```
┌─────────────────────────────────────────────────────────┐
│           Workflow Orchestration Engine                 │
│  (Lightweight coordinator, executes JSON workflows)     │
└─────────────────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┬──────────────┐
        ▼               ▼               ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Email Service│ │Webhook Service│ │  API Service │ │Database Svc  │
│              │ │              │ │              │ │              │
│ • Templates  │ │ • Signatures │ │ • Polling    │ │ • Query      │
│ • Retries    │ │ • Retries    │ │ • Long wait  │ │ • Bulk ops   │
│ • Tracking   │ │ • Auth       │ │ • OAuth      │ │ • Migrations │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
```

**Benefits:**
- ✅ **Independent deployment**: Update email service without touching orchestrator
- ✅ **Language freedom**: Email service in C#, webhook in Go, AI in Python
- ✅ **Resource optimization**: Only AI service gets GPU nodes
- ✅ **Team ownership**: Different teams own different services
- ✅ **Feature flags**: Deploy new nodes as JSON, enable per-user
- ✅ **Easier testing**: Test email service in isolation

## JSON-Defined Microservice Nodes

Each microservice node is defined in JSON. The orchestrator doesn't know or care about implementation:

```json
{
  "id": "send-welcome-email",
  "type": "Microservice",
  "service": "email-service",
  "operation": "send-template",
  "inputs": {
    "template": "welcome-email",
    "to": "{{user.email}}",
    "data": {
      "userName": "{{user.name}}",
      "activationUrl": "{{activation.url}}"
    }
  },
  "timeout": 30000,
  "retry": {
    "maxAttempts": 3,
    "backoffMs": 1000
  },
  "outputs": {
    "messageId": "{{response.messageId}}",
    "status": "{{response.status}}"
  },
  "onError": "handle-email-failure"
}
```

**Key Fields:**
- `service`: Which microservice to call
- `operation`: What operation to perform
- `inputs`: Data to send (with template variables)
- `timeout`: How long to wait for response
- `retry`: Retry configuration
- `outputs`: How to map response back to workflow context

## Service Registry

The orchestrator needs to know where these microservices live:

```csharp
public class ServiceRegistry
{
    private readonly Dictionary<string, ServiceDefinition> _services = new();

    public void RegisterService(string name, ServiceDefinition definition)
    {
        _services[name] = definition;
    }

    public ServiceDefinition GetService(string name)
    {
        if (!_services.TryGetValue(name, out var service))
            throw new InvalidOperationException($"Service {name} not registered");

        return service;
    }
}

public class ServiceDefinition
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string[] SupportedOperations { get; set; } = Array.Empty<string>();
    public AuthConfig? Auth { get; set; }
    public int DefaultTimeout { get; set; } = 30000;
    public HealthCheckConfig? HealthCheck { get; set; }
}

public class AuthConfig
{
    public string Type { get; set; } = "ApiKey"; // ApiKey, JWT, OAuth
    public Dictionary<string, string> Config { get; set; } = new();
}
```

**Registration (in appsettings.json or database):**

```json
{
  "WorkflowServices": {
    "email-service": {
      "baseUrl": "http://email-service:8080",
      "supportedOperations": ["send-template", "send-raw", "get-status"],
      "auth": {
        "type": "ApiKey",
        "config": {
          "header": "X-API-Key",
          "key": "secret-key-here"
        }
      },
      "healthCheck": {
        "endpoint": "/health",
        "intervalSeconds": 60
      }
    },
    "webhook-service": {
      "baseUrl": "http://webhook-service:8080",
      "supportedOperations": ["call", "call-signed"],
      "auth": {
        "type": "JWT",
        "config": {
          "secret": "jwt-secret"
        }
      }
    }
  }
}
```

## Microservice Node Executor

Now the orchestrator needs to execute microservice nodes:

```csharp
public class MicroserviceNodeExecutor
{
    private readonly ServiceRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MicroserviceNodeExecutor> _logger;

    public async Task<NodeExecutionResult> ExecuteAsync(
        WorkflowNode node,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        // Get service definition
        var serviceName = node.Inputs.GetValueOrDefault("service")?.ToString()
            ?? throw new ArgumentException("Service name not specified");

        var service = _registry.GetService(serviceName);

        // Get operation
        var operation = node.Inputs.GetValueOrDefault("operation")?.ToString()
            ?? throw new ArgumentException("Operation not specified");

        if (!service.SupportedOperations.Contains(operation))
            throw new InvalidOperationException(
                $"Service {serviceName} does not support operation {operation}");

        // Prepare request
        var requestData = PrepareRequestData(node, context);
        var timeout = node.Inputs.TryGetValue("timeout", out var t)
            ? Convert.ToInt32(t)
            : service.DefaultTimeout;

        // Execute with retry
        var retryConfig = ParseRetryConfig(node);
        var result = await ExecuteWithRetryAsync(
            service,
            operation,
            requestData,
            timeout,
            retryConfig,
            cancellationToken);

        return result;
    }

    private async Task<NodeExecutionResult> ExecuteWithRetryAsync(
        ServiceDefinition service,
        string operation,
        object requestData,
        int timeout,
        RetryConfig retryConfig,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < retryConfig.MaxAttempts)
        {
            try
            {
                attempt++;

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMilliseconds(timeout);

                // Add auth headers
                AddAuthHeaders(client, service.Auth);

                // Make request
                var url = $"{service.BaseUrl}/operations/{operation}";
                var response = await client.PostAsJsonAsync(
                    url,
                    requestData,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseData = await response.Content
                    .ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken);

                return new NodeExecutionResult
                {
                    Success = true,
                    Data = responseData ?? new(),
                    ExecutionTime = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex) when (attempt < retryConfig.MaxAttempts)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {Service}.{Operation}",
                    attempt, retryConfig.MaxAttempts, service.Name, operation);

                // Exponential backoff
                var delay = retryConfig.BackoffMs * Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
            }
        }

        // All retries exhausted
        return new NodeExecutionResult
        {
            Success = false,
            Error = $"Failed after {retryConfig.MaxAttempts} attempts: {lastException?.Message}",
            ExecutionTime = DateTimeOffset.UtcNow
        };
    }

    private void AddAuthHeaders(HttpClient client, AuthConfig? auth)
    {
        if (auth == null) return;

        switch (auth.Type)
        {
            case "ApiKey":
                var header = auth.Config["header"];
                var key = auth.Config["key"];
                client.DefaultRequestHeaders.Add(header, key);
                break;

            case "JWT":
                var token = GenerateJwt(auth.Config["secret"]);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                break;
        }
    }
}
```

**Key Features:**
- Automatic retry with exponential backoff
- Timeout handling
- Authentication (API key, JWT, OAuth)
- Error handling and logging

## Building a Microservice: Email Service

Let's build one of these microservices. Here's a simple email service in ASP.NET Core:

```csharp
// EmailService/Controllers/OperationsController.cs
[ApiController]
[Route("operations")]
public class OperationsController : ControllerBase
{
    private readonly IEmailSender _emailSender;
    private readonly ITemplateRenderer _templateRenderer;

    [HttpPost("send-template")]
    public async Task<IActionResult> SendTemplate(
        [FromBody] SendTemplateRequest request)
    {
        try
        {
            // Render template
            var body = await _templateRenderer.RenderAsync(
                request.Template,
                request.Data);

            // Send email
            var messageId = await _emailSender.SendAsync(
                request.To,
                request.Subject ?? "No Subject",
                body);

            return Ok(new
            {
                success = true,
                messageId,
                status = "sent",
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("send-raw")]
    public async Task<IActionResult> SendRaw(
        [FromBody] SendRawRequest request)
    {
        var messageId = await _emailSender.SendAsync(
            request.To,
            request.Subject,
            request.Body);

        return Ok(new { success = true, messageId });
    }

    [HttpGet("status/{messageId}")]
    public async Task<IActionResult> GetStatus(string messageId)
    {
        var status = await _emailSender.GetStatusAsync(messageId);
        return Ok(new { messageId, status });
    }
}

public class SendTemplateRequest
{
    public string Template { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
```

This service can be deployed independently, scaled independently, and updated without touching the workflow engine!

## Feature Switching Through JSON Deployment

Here's where it gets really powerful. You can deploy new node types just by uploading JSON:

```csharp
[HttpPost("workflow-capabilities")]
public async Task<IActionResult> DeployCapability(
    [FromBody] CapabilityDefinition capability)
{
    // Validate
    if (!await ValidateCapabilityAsync(capability))
        return BadRequest("Invalid capability definition");

    // Store in database
    var entity = new WorkflowCapabilityEntity
    {
        Name = capability.Name,
        Service = capability.Service,
        Operation = capability.Operation,
        SchemaJson = JsonSerializer.Serialize(capability.Schema),
        EnabledForUsers = capability.EnabledForUsers ?? new List<string>(),
        IsPublic = capability.IsPublic,
        Version = capability.Version
    };

    await _context.WorkflowCapabilities.AddAsync(entity);
    await _context.SaveChangesAsync();

    return Ok(new { success = true, capabilityId = entity.Id });
}

public class CapabilityDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public NodeSchema Schema { get; set; } = new();
    public List<string>? EnabledForUsers { get; set; } // null = all users
    public bool IsPublic { get; set; } = false;
    public string Version { get; set; } = "1.0.0";
}

public class NodeSchema
{
    public Dictionary<string, ParameterDefinition> Inputs { get; set; } = new();
    public Dictionary<string, ParameterDefinition> Outputs { get; set; } = new();
}

public class ParameterDefinition
{
    public string Type { get; set; } = "string"; // string, number, boolean, object
    public bool Required { get; set; } = false;
    public string? Description { get; set; }
    public object? DefaultValue { get; set; }
}
```

**Example Capability Deployment:**

```json
{
  "name": "Slack Notification",
  "description": "Send formatted message to Slack channel",
  "service": "slack-service",
  "operation": "send-message",
  "schema": {
    "inputs": {
      "channel": {
        "type": "string",
        "required": true,
        "description": "Slack channel ID or name"
      },
      "message": {
        "type": "string",
        "required": true,
        "description": "Message text (supports markdown)"
      },
      "attachments": {
        "type": "object",
        "required": false,
        "description": "Slack message attachments"
      }
    },
    "outputs": {
      "messageId": {
        "type": "string",
        "description": "Slack message timestamp"
      },
      "permalink": {
        "type": "string",
        "description": "Permanent link to message"
      }
    }
  },
  "enabledForUsers": ["admin@example.com", "user@example.com"],
  "isPublic": false,
  "version": "1.0.0"
}
```

Now, when users open the workflow editor, they see "Slack Notification" as an available node type - **no code deployment required!**

## Visual Editor Updates

The editor needs to show available capabilities:

```javascript
// Fetch available capabilities for current user
async function loadCapabilities() {
    const response = await fetch('/api/workflow-capabilities');
    const capabilities = await response.json();

    // Group by service
    const grouped = capabilities.reduce((acc, cap) => {
        if (!acc[cap.service]) acc[cap.service] = [];
        acc[cap.service].push(cap);
        return acc;
    }, {});

    // Render in sidebar
    renderCapabilitiesPanel(grouped);
}

function renderCapabilitiesPanel(capabilities) {
    const html = Object.entries(capabilities).map(([service, caps]) => `
        <div class="capability-group">
            <h3>${service}</h3>
            ${caps.map(cap => `
                <div class="capability-item"
                     draggable="true"
                     data-capability="${cap.name}">
                    <span class="capability-icon">📦</span>
                    <div>
                        <div class="capability-name">${cap.name}</div>
                        <div class="capability-desc">${cap.description}</div>
                    </div>
                </div>
            `).join('')}
        </div>
    `).join('');

    document.getElementById('capabilities-panel').innerHTML = html;
}
```

## Long-Running Operations

Some operations take minutes or hours. For these, use async callbacks:

```json
{
  "id": "train-ml-model",
  "type": "Microservice",
  "service": "ml-service",
  "operation": "train-model",
  "async": true,
  "callbackUrl": "{{workflow.callbackUrl}}",
  "inputs": {
    "dataset": "{{context.datasetId}}",
    "algorithm": "random-forest",
    "parameters": {
      "n_estimators": 100,
      "max_depth": 10
    }
  }
}
```

The ML service starts training and immediately returns:

```json
{
  "success": true,
  "jobId": "ml-job-12345",
  "status": "running",
  "estimatedDuration": 3600
}
```

When training completes, it calls back:

```csharp
[HttpPost("workflow/callback/{executionId}/{nodeId}")]
public async Task<IActionResult> NodeCallback(
    int executionId,
    string nodeId,
    [FromBody] Dictionary<string, object> result)
{
    // Resume workflow with result
    await _workflowEngine.ResumeNodeAsync(executionId, nodeId, result);
    return Ok();
}
```

## BPML Support

For true enterprise workflows, you could support BPML (Business Process Modeling Language) as an alternative to JSON:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL">
  <bpmn:process id="OrderFulfillment" name="Order Fulfillment">
    <bpmn:startEvent id="OrderReceived" name="Order Received"/>
    <bpmn:serviceTask id="ValidateInventory"
                      name="Validate Inventory"
                      implementation="microservice">
      <bpmn:extensionElements>
        <service>inventory-service</service>
        <operation>check-availability</operation>
      </bpmn:extensionElements>
    </bpmn:serviceTask>
    <bpmn:exclusiveGateway id="InventoryGateway"/>
    <bpmn:serviceTask id="ShipOrder"
                      name="Ship Order"
                      implementation="microservice">
      <bpmn:extensionElements>
        <service>shipping-service</service>
        <operation>create-shipment</operation>
      </bpmn:extensionElements>
    </bpmn:serviceTask>
  </bpmn:process>
</bpmn:definitions>
```

This would require a BPML parser that converts to your internal workflow format.

## Monitoring and Observability

With microservices, observability becomes critical:

```csharp
public class MicroserviceMetrics
{
    private static readonly Counter ServiceCalls = Metrics
        .CreateCounter("workflow_service_calls_total",
            "Total calls to microservices",
            new CounterConfiguration
            {
                LabelNames = new[] { "service", "operation", "status" }
            });

    private static readonly Histogram ServiceDuration = Metrics
        .CreateHistogram("workflow_service_duration_seconds",
            "Duration of microservice calls",
            new HistogramConfiguration
            {
                LabelNames = new[] { "service", "operation" }
            });

    public static void RecordServiceCall(
        string service,
        string operation,
        string status,
        TimeSpan duration)
    {
        ServiceCalls.WithLabels(service, operation, status).Inc();
        ServiceDuration.WithLabels(service, operation).Observe(duration.TotalSeconds);
    }
}
```

## MassTransit: The Game Changer

Now here's where things get *really* interesting. Instead of direct HTTP calls between the orchestrator and microservices, use **MassTransit** for message-based communication. This gives you:

- ✅ **Automatic retries** with exponential backoff
- ✅ **Saga state machines** for complex workflows
- ✅ **Message persistence** - no lost operations
- ✅ **Load balancing** across service instances
- ✅ **Distributed tracing** out of the box
- ✅ **Multiple transport options** (RabbitMQ, Azure Service Bus, Amazon SQS)

### Setting Up MassTransit

First, install the packages:

```bash
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
```

Configure in `Program.cs`:

```csharp
builder.Services.AddMassTransit(x =>
{
    // Add consumers for handling responses
    x.AddConsumer<NodeExecutionResponseConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure automatic retry
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromMinutes(5),
            intervalDelta: TimeSpan.FromSeconds(2)));

        // Configure circuit breaker
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

### Message-Based Node Execution

Define your messages:

```csharp
// Workflow orchestrator sends this
public record ExecuteNodeCommand
{
    public int WorkflowExecutionId { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public Dictionary<string, object> Inputs { get; init; } = new();
    public Guid CorrelationId { get; init; }
}

// Microservice responds with this
public record NodeExecutionResponse
{
    public int WorkflowExecutionId { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public Dictionary<string, object> Outputs { get; init; } = new();
    public string? Error { get; init; }
    public Guid CorrelationId { get; init; }
}
```

### Orchestrator Sends Work

Instead of HTTP calls, publish messages:

```csharp
public class MassTransitNodeExecutor
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRequestClient<ExecuteNodeCommand> _requestClient;
    private readonly ILogger<MassTransitNodeExecutor> _logger;

    public async Task<NodeExecutionResult> ExecuteAsync(
        WorkflowNode node,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var command = new ExecuteNodeCommand
        {
            WorkflowExecutionId = context["executionId"] as int? ?? 0,
            NodeId = node.Id,
            Operation = node.Inputs["operation"]?.ToString() ?? "",
            Inputs = node.Inputs,
            CorrelationId = Guid.NewGuid()
        };

        try
        {
            // Request-response pattern with timeout
            var response = await _requestClient.GetResponse<NodeExecutionResponse>(
                command,
                cancellationToken,
                timeout: RequestTimeout.After(m: 5));

            var result = response.Message;

            return new NodeExecutionResult
            {
                Success = result.Success,
                Data = result.Outputs,
                Error = result.Error
            };
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex,
                "Timeout executing node {NodeId} for workflow {WorkflowId}",
                node.Id, command.WorkflowExecutionId);

            return new NodeExecutionResult
            {
                Success = false,
                Error = "Operation timed out"
            };
        }
    }
}
```

### Microservice Consumes and Responds

Each microservice implements a consumer:

```csharp
// In email-service
public class EmailServiceConsumer : IConsumer<ExecuteNodeCommand>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailServiceConsumer> _logger;

    public async Task Consume(ConsumeContext<ExecuteNodeCommand> context)
    {
        var command = context.Message;

        _logger.LogInformation(
            "Processing node {NodeId} operation {Operation}",
            command.NodeId, command.Operation);

        try
        {
            Dictionary<string, object> outputs = new();

            // Route to appropriate handler
            switch (command.Operation)
            {
                case "send-template":
                    var messageId = await SendTemplateEmailAsync(command.Inputs);
                    outputs["messageId"] = messageId;
                    outputs["status"] = "sent";
                    break;

                case "send-raw":
                    messageId = await SendRawEmailAsync(command.Inputs);
                    outputs["messageId"] = messageId;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown operation: {command.Operation}");
            }

            // Respond with success
            await context.RespondAsync(new NodeExecutionResponse
            {
                WorkflowExecutionId = command.WorkflowExecutionId,
                NodeId = command.NodeId,
                Success = true,
                Outputs = outputs,
                CorrelationId = command.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing node {NodeId}", command.NodeId);

            // Respond with error
            await context.RespondAsync(new NodeExecutionResponse
            {
                WorkflowExecutionId = command.WorkflowExecutionId,
                NodeId = command.NodeId,
                Success = false,
                Error = ex.Message,
                CorrelationId = command.CorrelationId
            });
        }
    }

    private async Task<string> SendTemplateEmailAsync(
        Dictionary<string, object> inputs)
    {
        var template = inputs["template"].ToString();
        var to = inputs["to"].ToString();
        var data = inputs["data"] as Dictionary<string, object>;

        return await _emailSender.SendTemplateAsync(template, to, data);
    }
}
```

**Configure the consumer:**

```csharp
// In email-service Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailServiceConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");

        // Listen on specific queue
        cfg.ReceiveEndpoint("email-service", e =>
        {
            e.ConfigureConsumer<EmailServiceConsumer>(context);

            // Concurrent message processing
            e.PrefetchCount = 20;
            e.ConcurrentMessageLimit = 10;
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

### Saga Pattern for Long-Running Workflows

For workflows that span hours or days, use MassTransit Sagas:

```csharp
public class WorkflowExecutionSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public int WorkflowExecutionId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WorkflowExecutionStateMachine : MassTransitStateMachine<WorkflowExecutionSaga>
{
    public WorkflowExecutionStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => WorkflowStarted, x =>
            x.CorrelateById(context => context.Message.CorrelationId));

        Event(() => NodeCompleted, x =>
            x.CorrelateById(context => context.Message.CorrelationId));

        Initially(
            When(WorkflowStarted)
                .Then(context =>
                {
                    context.Saga.WorkflowExecutionId =
                        context.Message.WorkflowExecutionId;
                    context.Saga.StartedAt = DateTime.UtcNow;
                })
                .TransitionTo(Running)
                .Publish(context => new ExecuteNextNodeCommand
                {
                    WorkflowExecutionId = context.Saga.WorkflowExecutionId,
                    CorrelationId = context.Saga.CorrelationId
                }));

        During(Running,
            When(NodeCompleted)
                .Then(context =>
                {
                    // Merge outputs into context
                    foreach (var (key, value) in context.Message.Outputs)
                    {
                        context.Saga.Context[key] = value;
                    }
                })
                .IfElse(context => HasMoreNodes(context.Saga),
                    // More nodes to execute
                    then => then.Publish(context => new ExecuteNextNodeCommand
                    {
                        WorkflowExecutionId = context.Saga.WorkflowExecutionId,
                        CorrelationId = context.Saga.CorrelationId
                    }),
                    // Workflow complete
                    @else => @else
                        .Then(context =>
                        {
                            context.Saga.CompletedAt = DateTime.UtcNow;
                        })
                        .TransitionTo(Completed)
                        .Finalize()));

        SetCompletedWhenFinalized();
    }

    public State Running { get; private set; } = null!;
    public State Completed { get; private set; } = null!;

    public Event<WorkflowStartedEvent> WorkflowStarted { get; private set; } = null!;
    public Event<NodeExecutionResponse> NodeCompleted { get; private set; } = null!;

    private bool HasMoreNodes(WorkflowExecutionSaga saga)
    {
        // Logic to determine if more nodes need execution
        return false; // Simplified
    }
}
```

**Register the saga:**

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<WorkflowExecutionStateMachine, WorkflowExecutionSaga>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<MostlylucidDbContext>();
            r.UsePostgres();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
        cfg.ConfigureEndpoints(context);
    });
});
```

### Why MassTransit Makes This System Super Adaptable

**1. Decoupling**: Services don't need to know about each other's locations.

**2. Resilience**: Messages are persisted - if a service crashes, messages wait until it recovers.

**3. Scalability**: Add more consumer instances for automatic load balancing:
```bash
# Run 5 instances of email-service
docker-compose scale email-service=5
```

**4. Observability**: MassTransit integrates with OpenTelemetry for distributed tracing:
```csharp
cfg.UseOpenTelemetry(tracing =>
{
    tracing.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("workflow-orchestrator"));
});
```

**5. Multiple Transports**: Swap RabbitMQ for Azure Service Bus without changing business logic:
```csharp
x.UsingAzureServiceBus((context, cfg) =>
{
    cfg.Host("your-connection-string");
    cfg.ConfigureEndpoints(context);
});
```

**6. Scheduled Messages**: Delay node execution:
```csharp
await context.SchedulePublish(
    TimeSpan.FromHours(24),
    new ExecuteNodeCommand { /* ... */ });
```

**7. Competing Consumers**: Multiple services can handle the same message type with automatic load balancing.

**8. Message Routing**: Route messages based on content:
```csharp
cfg.Send<ExecuteNodeCommand>(s =>
{
    // Route email operations to email-service queue
    s.UseRoutingKeyFormatter(context =>
        context.Message.Operation.StartsWith("email")
            ? "email-service"
            : "default-service");
});
```

### Complete Architecture with MassTransit

```
┌─────────────────────────────────────────────┐
│     Workflow Orchestrator                   │
│  - Publishes ExecuteNodeCommand messages    │
│  - Saga for long-running workflows          │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│            RabbitMQ / Azure Service Bus     │
│  - Message persistence                      │
│  - Automatic retry and dead-letter          │
│  - Load balancing                           │
└─────────────────┬───────────────────────────┘
                  │
        ┌─────────┼─────────┬──────────┐
        ▼         ▼         ▼          ▼
    ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
    │Email   │ │Webhook │ │SMS     │ │AI      │
    │Service │ │Service │ │Service │ │Service │
    │(x5)    │ │(x3)    │ │(x2)    │ │(x2)    │
    └────────┘ └────────┘ └────────┘ └────────┘
```

Each service independently scales based on message queue depth!

## Suggested Improvements

Based on this architecture, here are some enhancements I'd recommend:

### 1. **Service Discovery**
Instead of hardcoding service URLs, use Consul or Eureka:

```csharp
public class ConsulServiceRegistry : ServiceRegistry
{
    private readonly IConsulClient _consul;

    public override ServiceDefinition GetService(string name)
    {
        var services = await _consul.Health.Service(name, "", true);
        var service = services.Response.First().Service;

        return new ServiceDefinition
        {
            Name = name,
            BaseUrl = $"http://{service.Address}:{service.Port}"
        };
    }
}
```

### 2. **Circuit Breaker Pattern**
Use Polly to prevent cascading failures:

```csharp
var policy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );

await policy.ExecuteAsync(() => CallServiceAsync(...));
```

### 3. **Request Tracing**
Add correlation IDs to track requests across services:

```csharp
client.DefaultRequestHeaders.Add("X-Correlation-Id", workflow.ExecutionId.ToString());
```

### 4. **Schema Validation**
Validate node inputs against capability schemas before execution:

```csharp
public bool ValidateNodeInputs(WorkflowNode node, CapabilityDefinition capability)
{
    foreach (var (key, def) in capability.Schema.Inputs)
    {
        if (def.Required && !node.Inputs.ContainsKey(key))
            return false;

        // Type validation
        if (node.Inputs.TryGetValue(key, out var value))
        {
            if (!IsCorrectType(value, def.Type))
                return false;
        }
    }

    return true;
}
```

### 5. **Versioning**
Support multiple versions of the same capability:

```json
{
  "name": "Send Email",
  "version": "2.0.0",
  "service": "email-service-v2",
  "deprecates": "1.0.0",
  "migrationGuide": "https://docs.example.com/email-v2-migration"
}
```

### 6. **Rate Limiting**
Prevent abuse of expensive operations:

```csharp
[RateLimit(PermitLimit = 100, Window = "1m")]
[HttpPost("operations/expensive-operation")]
public async Task<IActionResult> ExpensiveOperation(...)
{
    // ...
}
```

### 7. **Caching**
Cache microservice responses for idempotent operations:

```csharp
var cacheKey = $"{service}:{operation}:{hash(inputs)}";
if (_cache.TryGetValue(cacheKey, out var cached))
    return cached;

var result = await ExecuteServiceCallAsync(...);
_cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
```

### 8. **Workflow Marketplace**
Allow users to share workflow templates:

```csharp
[HttpPost("workflow-templates")]
public async Task<IActionResult> PublishTemplate(
    [FromBody] WorkflowTemplate template)
{
    template.AuthorId = User.GetUserId();
    template.PublishedAt = DateTime.UtcNow;

    await _context.WorkflowTemplates.AddAsync(template);
    await _context.SaveChangesAsync();

    return Ok(new { templateId = template.Id });
}
```

### 9. **Conditional Service Selection**
Route to different services based on load or region:

```json
{
  "type": "Microservice",
  "serviceSelection": {
    "strategy": "round-robin",
    "services": [
      "email-service-us-east",
      "email-service-us-west",
      "email-service-eu"
    ]
  }
}
```

### 10. **Rollback Capabilities**
For workflows that modify state, support compensation:

```json
{
  "id": "create-order",
  "type": "Microservice",
  "service": "order-service",
  "operation": "create",
  "compensation": {
    "operation": "cancel",
    "inputs": {
      "orderId": "{{outputs.orderId}}"
    }
  }
}
```

## Real-World Architecture Example

Here's what a complete system might look like:

```
Production Environment:
├── Orchestration Cluster (3 nodes)
│   ├── Workflow Engine API
│   ├── Hangfire Workers
│   └── PostgreSQL (workflow definitions, execution state)
│
├── Service Mesh (Istio/Linkerd)
│   ├── email-service (5 replicas)
│   ├── webhook-service (3 replicas)
│   ├── sms-service (2 replicas)
│   ├── pdf-service (2 replicas)
│   ├── ai-service (2 replicas, GPU nodes)
│   └── database-service (3 replicas)
│
├── Infrastructure Services
│   ├── Consul (service discovery)
│   ├── Vault (secrets management)
│   ├── Prometheus (metrics)
│   ├── Grafana (dashboards)
│   ├── Jaeger (distributed tracing)
│   └── Seq (centralized logging)
│
└── API Gateway (Kong/Traefik)
    └── External workflow triggers
```

## Performance Considerations

**Latency**: Each microservice call adds network overhead. For simple operations, stick with built-in nodes.

**Solution**: Hybrid approach
```csharp
public async Task<NodeExecutionResult> ExecuteNodeAsync(WorkflowNode node)
{
    // Use built-in executor for simple operations
    if (node.Type is "Log" or "Transform" or "Condition")
        return await _builtInExecutor.ExecuteAsync(node);

    // Use microservice for complex operations
    return await _microserviceExecutor.ExecuteAsync(node);
}
```

**Throughput**: Too many service calls can overwhelm the network.

**Solution**: Batch operations
```json
{
  "type": "Microservice",
  "service": "email-service",
  "operation": "send-batch",
  "inputs": {
    "messages": [
      {"to": "user1@example.com", "template": "welcome"},
      {"to": "user2@example.com", "template": "welcome"},
      {"to": "user3@example.com", "template": "welcome"}
    ]
  }
}
```

## Conclusion

We've transformed our monolithic workflow system into a powerful, extensible orchestration platform:

✅ **Microservice-based nodes** - Each complex operation is a separate service
✅ **JSON deployment** - Add new capabilities without code changes
✅ **Feature flags** - Enable features per-user or per-team
✅ **Independent scaling** - Scale each service based on demand
✅ **Language freedom** - Build services in any language
✅ **Team autonomy** - Teams own their services
✅ **Visual design** - Drag, drop, and configure
✅ **Enterprise-ready** - Supports BPML, retries, circuit breakers

This architecture gives you the best of both worlds: the simplicity of a visual workflow builder with the power and flexibility of microservices. Users design workflows visually, but underneath, you have a sophisticated distributed system that can handle anything from simple email notifications to complex ML pipelines.

## The Complete Series

- **[Part 1](/blog/workflowsystem-part1-introduction)**: Introduction and architecture
- **[Part 2](#)**: Core workflow engine
- **[Part 3](/blog/workflowsystem-part3-visual-editor)**: Visual workflow editor
- **[Part 4](/blog/workflowsystem-part4-hangfire)**: Hangfire integration
- **Part 5**: Breaking the monolith (this post)

## What's Next?

Some exciting directions to explore:

- **Sub-workflows**: Compose workflows from other workflows
- **Human-in-the-loop**: Approval nodes that pause for user input
- **Workflow analytics**: Track which paths are most common, where failures occur
- **A/B testing**: Run two workflow variants and compare results
- **Workflow versioning**: Update workflows without breaking running instances
- **Multi-tenancy**: Isolate workflows per customer

The possibilities are endless once you have this foundation!

## Source Code

All code from this series is available in the repository at:
- Workflow engine: `Mostlylucid.Workflow.Engine/`
- Microservice executor: `Mostlylucid.Workflow.Engine/Execution/MicroserviceNodeExecutor.cs`
- Service registry: `Mostlylucid.Workflow.Shared/ServiceRegistry.cs`

Happy workflow building! 🚀
