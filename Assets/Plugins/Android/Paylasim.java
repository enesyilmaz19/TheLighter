package com.thelighter.paylasim;

import android.app.Activity;
import android.content.ClipData;
import android.content.Intent;
import android.net.Uri;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

/**
 * Paylasim kartini telefonun paylasma menusuyle (ACTION_SEND) paylasir: Instagram, WhatsApp...
 * Unity'den cagrilir: Assets/Scripts/Cekirdek/Paylasim.cs. Resim onbellekteki "paylasim" klasorune kopyalanir ve
 * PaylasimSaglayici uzerinden, sadece bu paylasim icin, salt okunur verilir.
 */
public final class Paylasim {
    private Paylasim() {
    }

    /** Menu acilacaksa true. Dosya yoksa ya da kopyalanamazsa false. */
    public static boolean resimPaylas(final Activity aktivite, String yol, final String metin, final String baslik) {
        try {
            File kaynak = new File(yol);
            if (!kaynak.isFile()) return false;
            File klasor = PaylasimSaglayici.klasor(aktivite);
            if (!klasor.isDirectory() && !klasor.mkdirs()) return false;
            // Eski kartlar birikmesin: klasorde sadece sonuncusu kalir.
            File[] eskiler = klasor.listFiles();
            if (eskiler != null) {
                for (File f : eskiler) {
                    //noinspection ResultOfMethodCallIgnored
                    f.delete();
                }
            }
            File hedef = new File(klasor, kaynak.getName());
            kopyala(kaynak, hedef);
            final Uri uri = Uri.parse("content://" + aktivite.getPackageName() + PaylasimSaglayici.EK + "/" + Uri.encode(hedef.getName()));
            aktivite.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    try {
                        Intent gonder = new Intent(Intent.ACTION_SEND);
                        gonder.setType("image/png");
                        gonder.putExtra(Intent.EXTRA_STREAM, uri);
                        if (metin != null && metin.length() > 0) gonder.putExtra(Intent.EXTRA_TEXT, metin);
                        gonder.setClipData(ClipData.newRawUri("", uri));
                        gonder.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                        Intent secici = Intent.createChooser(gonder, baslik);
                        secici.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                        aktivite.startActivity(secici);
                    } catch (Exception e) {
                        // Paylasacak uygulama yoksa: sessizce gec, oyun surer.
                    }
                }
            });
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    private static void kopyala(File kaynak, File hedef) throws IOException {
        InputStream giris = new FileInputStream(kaynak);
        try {
            OutputStream cikis = new FileOutputStream(hedef);
            try {
                byte[] tampon = new byte[16384];
                int n;
                while ((n = giris.read(tampon)) > 0) cikis.write(tampon, 0, n);
            } finally {
                cikis.close();
            }
        } finally {
            giris.close();
        }
    }
}
