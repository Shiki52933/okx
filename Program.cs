var apiKey = "b8096cac-ad15-48a6-ac1b-21200f807921";
var apiSecretKey = "DE1AFCA608CC0E536EBBB42663C53B5F";
var passphrase = "Arcueid_001";

var client = new okx.Client(apiKey, apiSecretKey, passphrase);
client.requestIndexkLine("BTC-USD", limit: 10);
