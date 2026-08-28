using System;
using System.Windows.Forms;
using System.Drawing;

namespace Arquimedes
{
    public partial class FrmExito : Form
    {
        public FrmExito(string nombreCompleto, Bitmap qrImage)
        {
            InitializeComponent();
            label1.Text = $" El alumno {nombreCompleto} fue agregado exitosamente.";
            pictureBoxQR.Image = qrImage;
            pictureBoxQR.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}