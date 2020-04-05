<Query Kind="Statements">
  <Namespace>System.Net.Mail</Namespace>
</Query>

var client = new SmtpClient()
{
	Host = "localhost",
	EnableSsl = false,
	Port = 2500,
	DeliveryMethod = SmtpDeliveryMethod.Network,
	UseDefaultCredentials = true,
//	Credentials = new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"]),
	Timeout = 100000
};
client.Send("test@example.com", "to@example.com", "Test", "Body");