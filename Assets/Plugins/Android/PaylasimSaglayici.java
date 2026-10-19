package com.thelighter.paylasim;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileNotFoundException;
import java.util.ArrayList;

/**
 * Paylasilan karti baska bir uygulamaya salt okunur verir: sadece onbellekteki "paylasim" klasorunden, sadece dosya
 * adiyla (alt klasor ve ".." yok). Disa kapali (exported=false); okuma izni paylasma niyetiyle tek seferlik verilir.
 * Manifeste Assets/Scripts/Editor/AndroidPaylasimManifesti.cs ekler: authorities = paket adi + ".paylasim".
 */
public final class PaylasimSaglayici extends ContentProvider {
    static final String EK = ".paylasim";

    static File klasor(Context c) {
        return new File(c.getCacheDir(), "paylasim");
    }

    @Override
    public boolean onCreate() {
        return true;
    }

    private File dosya(Uri uri) throws FileNotFoundException {
        Context c = getContext();
        String ad = uri.getLastPathSegment();
        if (c == null || ad == null || ad.length() == 0 || ad.contains("/") || ad.contains("\\") || ad.startsWith(".")) {
            throw new FileNotFoundException();
        }
        File f = new File(klasor(c), ad);
        if (!f.isFile()) throw new FileNotFoundException();
        return f;
    }

    @Override
    public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        if (!"r".equals(mode)) throw new FileNotFoundException("salt okunur");
        return ParcelFileDescriptor.open(dosya(uri), ParcelFileDescriptor.MODE_READ_ONLY);
    }

    @Override
    public String getType(Uri uri) {
        return "image/png";
    }

    /** Bazi uygulamalar adi ve boyutu sorar (OpenableColumns). */
    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        File f;
        try {
            f = dosya(uri);
        } catch (FileNotFoundException e) {
            return null;
        }
        String[] istenen = projection != null ? projection : new String[] { OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE };
        ArrayList<String> sutunlar = new ArrayList<String>();
        ArrayList<Object> degerler = new ArrayList<Object>();
        for (String s : istenen) {
            if (OpenableColumns.DISPLAY_NAME.equals(s)) {
                sutunlar.add(s);
                degerler.add(f.getName());
            } else if (OpenableColumns.SIZE.equals(s)) {
                sutunlar.add(s);
                degerler.add(f.length());
            }
        }
        MatrixCursor imlec = new MatrixCursor(sutunlar.toArray(new String[0]), 1);
        imlec.addRow(degerler.toArray());
        return imlec;
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        throw new UnsupportedOperationException();
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException();
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException();
    }
}
