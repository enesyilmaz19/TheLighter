using System;
using System.Security.Cryptography;

namespace Cakmak.Sunucu
{
    /// <summary>
    /// Kural motorunun online rastgelesi, işletim sisteminin kriptografik kaynağından. Tohumlu <see cref="Random"/>
    /// kullanılsaydı tohum oyunda görülen sonuçlardan (zarlar, ilk A, karışık mini oyun) geri bulunabilir, bulan
    /// herkesin gizli görevini, Kızgın Çakmak eşiğini ve gelecek zarları bilirdi. Kural motoru sadece Next'leri
    /// çağırıyor; Random'ın türetilmiş sınıfta kullandığı Sample ve NextBytes de buraya bağlı.
    /// </summary>
    sealed class GuvenliRastgele : Random
    {
        public override int Next() => RandomNumberGenerator.GetInt32(int.MaxValue);

        public override int Next(int enFazla)
        {
            if (enFazla < 0) throw new ArgumentOutOfRangeException(nameof(enFazla));
            return enFazla == 0 ? 0 : RandomNumberGenerator.GetInt32(enFazla);
        }

        public override int Next(int enAz, int enFazla)
        {
            if (enAz > enFazla) throw new ArgumentOutOfRangeException(nameof(enAz));
            return enAz == enFazla ? enAz : RandomNumberGenerator.GetInt32(enAz, enFazla);
        }

        public override double NextDouble() => Sample();

        /// <summary>[0, 1): 53 rastgele bit.</summary>
        protected override double Sample()
        {
            Span<byte> b = stackalloc byte[8];
            RandomNumberGenerator.Fill(b);
            return (BitConverter.ToUInt64(b) >> 11) * (1.0 / (1UL << 53));
        }

        public override void NextBytes(byte[] buffer) => RandomNumberGenerator.Fill(buffer);

        public override void NextBytes(Span<byte> buffer) => RandomNumberGenerator.Fill(buffer);
    }
}
