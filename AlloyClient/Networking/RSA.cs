using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using System;
using System.IO;
using System.Text;

namespace AlloyClient.Networking;

public static class Rsa
{
    private static readonly AsymmetricKeyParameter PublicKey;

    static Rsa()
    {
        const string pem =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MIGeMA0GCSqGSIb3DQEBAQUAA4GMADCBiAKBgGqM3INzNpNSfrEe0H6EY+KxM7vq\n" +
            "0utjxfzDTVJLF/BKahWFZ0W1+fuwA6oKuToHvla2FjSbVaNiT2ablQPib3Y+j1q4\n" +
            "ndY9EpKykHGfbIYyeFwHEZSk76AuGU9Kxwneq39M16vHAYA75d3R5dEUCq7bCY6j\n" +
            "mS1uS9lPPiDJ5513AgMBAAE=\n" +
            "-----END PUBLIC KEY-----";

        PublicKey = (AsymmetricKeyParameter)new PemReader(new StringReader(pem)).ReadObject();
    }

    public static string EncryptPublic(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        var encoding = new Pkcs1Encoding(new RsaEngine());
        encoding.Init(true, PublicKey);
        var bytes = Encoding.UTF8.GetBytes(str);
        return Convert.ToBase64String(encoding.ProcessBlock(bytes, 0, bytes.Length));
    }
}