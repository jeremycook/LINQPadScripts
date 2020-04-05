<Query Kind="Statements">
  <NuGetReference>QRCoder</NuGetReference>
  <NuGetReference>SimpleBase</NuGetReference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>QRCoder</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

var chars = new[]
{
	'0',
	'1',
	'2',
	'3',
	'4',
	'5',
	'6',
	'7',
	'8',
	'9',
	'A',
	'B',
	'C',
	'D',
	'E',
	'F',
	'G',
	'H',
	'J',
	'K',
	'M',
	'N',
	'P',
	'Q',
	'R',
	'S',
	'T',
	'V',
	'W',
	'X',
	'Y',
	'Z',
};

var bits = Util.ReadLine<int>("Bits of Entropy", 16, new[] { 16, 32 });
var data = new byte[bits];
RandomNumberGenerator.Fill(data);

var code = SimpleBase.Base58.Bitcoin.Encode(data).Dump("Bitcoin encoded");
//data.Dump();
string.Concat(code.Select((d, i) => (i + 1) % 6 == 0 ? d + " " : d.ToString())).Dump();

QRCodeGenerator qrGenerator = new QRCodeGenerator();
QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
QRCode qrCode = new QRCode(qrCodeData);
Bitmap qrCodeImage = qrCode.GetGraphic(10).Dump();