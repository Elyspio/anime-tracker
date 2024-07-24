namespace AnimeTracker.Api.Adapters.MassTransit.Configurations;

public class RabbitMqConfiguration
{
	public const string Section = "RabbitMq";
	public required string[] Nodes { get; set; }
	public required string Username { get; set; }
	public required string Password { get; set; }
	public required string VirtualHost { get; set; }
	
}