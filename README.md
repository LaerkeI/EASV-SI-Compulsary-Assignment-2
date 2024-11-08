# EASV-SI-Compulsary-Assignment-2

## Week 45 - Reliable Microservices

The following mitigations have been implemented: 

**Healthchecks** for monitoring the health of each microservice.
By adding condition:service_healthy in the depends_on section of a microservice in docker-compose.yml, 
it is guaranteed that the "depender" microservice does not start before the "dependee" microservice is healthy.  

**ocelot.json Rate Limits and Circuit Breakers** for limiting requests per user and protecting against abuse.
In *ocelot.json*, QoSOptions are used for configuring circuit breakers and retries. 
```
    {
        "RateLimitOptions": {
            "ClientWhitelist": [],
            "EnableRateLimiting": true,
            "Period": "1m",
            "PeriodTimespan": 60,
            "Limit": 100
        },
        "QoSOptions": {
            "ExceptionsAllowedBeforeBreaking": 3,
            "DurationOfBreak": 10000,
            "TimeoutValue": 5000
        }
    }
```

**ApiGateway Retry Logic, Circuit Breaker and Fallback Logic**
In *Program.cs* ín ApiGateway, Polly is being added to handle transient failures with retry, circuit breaker, and fallback policies. 
And Ocelot is being configured to use the HttpClient "OcelotHttpClient" that has been configured with Polly policies.  
```
    builder.Services.AddHttpClient("OcelotHttpClient")
        .AddTransientHttpErrorPolicy(policyBuilder => 
            policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500)))
        .AddTransientHttpErrorPolicy(policyBuilder =>
            policyBuilder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
        .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<HttpRequestException>()
            .FallbackAsync(async cancellationToken =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"message\": \"Service temporarily unavailable\"}",
                        Encoding.UTF8, "application/json")
                };
            }));

    builder.Services.AddOcelot(builder.Configuration).AddPolly();
```

**TimelineServiceHttpClient for Retry Logic and Circuit Breaker**
The HttpClient in TimelineService that sends requests to UserManagementService and to PostManagementService in order to create the user feed
is configured with Polly policies as well.
```
    builder.Services.AddHttpClient("TimelineServiceClient")
        .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500)))
        .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```
