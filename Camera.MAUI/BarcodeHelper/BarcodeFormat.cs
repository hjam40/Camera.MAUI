namespace Camera.MAUI;

//
// Summary:
//     Enumerates barcode formats known to this package.
[Flags]
public enum BarcodeFormat
{
    //
    // Summary:
    //     Aztec 2D barcode format.
    AZTEC = 0x1,

    //
    // Summary:
    //     CODABAR 1D format.
    CODABAR = 0x2,

    //
    // Summary:
    //     Code 39 1D format.
    CODE_39 = 0x4,

    //
    // Summary:
    //     Code 93 1D format.
    CODE_93 = 0x8,

    //
    // Summary:
    //     Code 128 1D format.
    CODE_128 = 0x10,

    //
    // Summary:
    //     Data Matrix 2D barcode format.
    DATA_MATRIX = 0x20,

    //
    // Summary:
    //     EAN-8 1D format.
    EAN_8 = 0x40,

    //
    // Summary:
    //     EAN-13 1D format.
    EAN_13 = 0x80,

    //
    // Summary:
    //     ITF (Interleaved Two of Five) 1D format.
    ITF = 0x100,

    //
    // Summary:
    //     MaxiCode 2D barcode format.
    MAXICODE = 0x200,

    //
    // Summary:
    //     PDF417 format.
    PDF_417 = 0x400,

    //
    // Summary:
    //     QR Code 2D barcode format.
    QR_CODE = 0x800,

    //
    // Summary:
    //     RSS 14
    RSS_14 = 0x1000,

    //
    // Summary:
    //     RSS EXPANDED
    RSS_EXPANDED = 0x2000,

    //
    // Summary:
    //     UPC-A 1D format.
    UPC_A = 0x4000,

    //
    // Summary:
    //     UPC-E 1D format.
    UPC_E = 0x8000,

    //
    // Summary:
    //     UPC/EAN extension format. Not a stand-alone format.
    UPC_EAN_EXTENSION = 0x10000,

    //
    // Summary:
    //     MSI
    MSI = 0x20000,

    //
    // Summary:
    //     Plessey
    PLESSEY = 0x40000,

    //
    // Summary:
    //     Intelligent Mail barcode
    IMB = 0x80000,

    //
    // Summary:
    //     Pharmacode format.
    PHARMA_CODE = 0x100000,

    //
    // Summary:
    //     UPC_A | UPC_E | EAN_13 | EAN_8 | CODABAR | CODE_39 | CODE_93 | CODE_128 | ITF
    //     | RSS_14 | RSS_EXPANDED without MSI (to many false-positives) and IMB (not enough
    //     tested, and it looks more like a 2D)
    All_1D = 0xF1DE
}