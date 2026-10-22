# SyZero.EventBus.RabbitMQ

SyZero.EventBus.RabbitMQ provides a distributed event-bus implementation based on RabbitMQ, including persistence and reconnect support.

## Install

```bash
dotnet add package SyZero.EventBus.RabbitMQ
```

## Key Features

- Distributed pub/sub event bus
- Message persistence support
- Automatic reconnect handling
- Retry and delayed-message scenarios

## Quick Start

```csharp
builder.Services.AddRabbitMQEventBus();
```

## Configuration Example (appsettings.json)

```json
{
	"RabbitMQ": {
		"HostName": "localhost",
		"Port": 5672,
		"UserName": "guest",
		"Password": "guest",
		"VirtualHost": "/",
		"ExchangeName": "syzero.exchange",
		"QueueName": "syzero.queue",
		"RetryCount": 3
	}
}
```

## Common Configuration

- RabbitMQ.HostName
- RabbitMQ.Port
- RabbitMQ.UserName
- RabbitMQ.Password
- RabbitMQ.VirtualHost
- RabbitMQ.ExchangeName
- RabbitMQ.QueueName
- RabbitMQ.RetryCount

## References

- Chinese documentation: [/modules/SyZero.EventBus.RabbitMQ](/modules/SyZero.EventBus.RabbitMQ)



