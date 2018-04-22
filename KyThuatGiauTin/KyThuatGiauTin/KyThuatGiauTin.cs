using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KyThuatGiauTin
{
    public partial class KyThuatGiauTin : Form
    {
        private String stPath, ndPath, decPath;
        private const int offset = 54;

        public KyThuatGiauTin()
        {
            InitializeComponent();
        }

        public static byte Extract(byte b, int x)
        {
            return (byte)((b & (1 << x)) >> x);
        }

        public static void Replace(ref byte b, int x, byte value)
        {
            b = (byte)(value == 1 ? b | (1 << x) : b & ~(1 << x));
        }

        public static void Encode(FileStream inStream, byte[] message, FileStream outStream)
        {
            int byteRead, i = 0, j = 0;
            byte byteWrite;
            while ((byteRead = inStream.ReadByte()) != -1)
            {
                byteWrite = (byte)byteRead;

                if (i < message.Length)
                {
                    byte bit = Extract(message[i], j++);
                    Replace(ref byteWrite, 0, bit);
                    if (j == 8)
                    {
                        j = 0;
                        i++;
                    }
                }
                outStream.WriteByte(byteWrite);
            }
            inStream.Close();
            outStream.Close();
        }

        public static byte[] Decode(FileStream inStream, int length)
        {
            int byteIndex = 0, bitIndex = 0, byteRead;
            byte[] result = new byte[length];
            while ((byteRead = inStream.ReadByte()) != -1)
            {
                byte bit = Extract((byte)byteRead, 0);
                Replace(ref result[byteIndex], bitIndex++, bit);
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
                if (byteIndex == length)
                    break;
            }
            return result;
        }

        private byte[] AddMessage(byte[] message)
        {
            int mes = message.Length;
            byte[] byteMes = BitConverter.GetBytes(mes);
            byte[] newMes = new byte[mes + byteMes.Length];
            for (int i = 0; i < byteMes.Length; i++)
                newMes[i] = byteMes[i];
            for (int i = 0; i < mes; i++)
                newMes[i + byteMes.Length] = message[i];
            return newMes;
        }

        public void CreateFile(string stPath, string ndPath, string message, string password)
        {
            FileStream inStream = new FileStream(stPath, FileMode.Open, FileAccess.Read);
            inStream.Seek(0, 0);
            byte[] header = new byte[offset];
            inStream.Read(header, 0, offset);
            FileStream outStream = new FileStream(ndPath, FileMode.Create, FileAccess.Write);
            outStream.Write(header, 0, offset);
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] newMessageByte = AddMessage(unicode.GetBytes(password + message));
            inStream.Seek(offset, 0);
            Encode(inStream, newMessageByte, outStream);
            inStream.Close();
            outStream.Close();
        }

        private void FixPicture(Image picImage, PictureBox picBox)
        {
            picBox.Image = picImage;
            if (picImage.Width < 512 && picImage.Height < 512)
            {
                picBox.Location = new Point((picBox.Parent.ClientSize.Width / 2) - (picImage.Width / 2),
                                            (picBox.Parent.ClientSize.Height / 2) - (picImage.Height / 2));
                picBox.SizeMode = PictureBoxSizeMode.Normal;
            }
            else
            {
                picBox.Location = new Point(0, 25);
                picBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        private byte[] ByteArray(byte[] stArr, byte[] ndArr)
        {
            byte[] resultArr = new byte[stArr.Length + ndArr.Length];
            for (int i = 0; i < stArr.Length; i++)
                resultArr[i] = stArr[i];
            for (int i = 0; i < ndArr.Length; i++)
                resultArr[i + stArr.Length] = ndArr[i];
            return resultArr;
        }

        private void btnChoose_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Chọn ảnh để giấu tin";
            openFile.Filter = "Bitmap Image(*.bmp)|*.bmp";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                stPath = openFile.FileName;
                Bitmap oldBitmap = new Bitmap(stPath);
                FixPicture(oldBitmap, pictureBefore);
                pictureBefore.Image = oldBitmap;
                txtPath.Text = stPath;
                txtPassword.Focus();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (txtPath.Text == "")
                MessageBox.Show("Vui lòng chọn ảnh để giấu tin!", "Thông báo");
            else if (txtPassword.Text == "")
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Thông báo");
            }
            else if (txtPassword.Text.Length < 8 || txtPassword.Text.Length > 16)
            {
                MessageBox.Show("Mật khẩu phải từ 8 -> 16 ký tự!", "Thông báo");
            }
            else if (txtPassword.Text.Contains(" "))
            {
                MessageBox.Show("Mật khẩu không được chứa khoảng trắng!", "Thông báo");
            }
            else if (txtConfirm.Text == "")
            {
                MessageBox.Show("Vui lòng nhập xác thực mật khẩu!", "Thông báo");
            }

            else if (txtPassword.Text != txtConfirm.Text)
            {
                MessageBox.Show("Mật khẩu xác thực không đúng!", "Thông báo");
            }
            else if (txtMessage.Text == "")
            {
                MessageBox.Show("Vui lòng nhập message!", "Thông báo");
            }
            else
            {
                String password = txtPassword.Text;
                for (int i = password.Length; i < 16; i++)
                {
                    password += " ";
                }
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Title = "Chọn nơi lưu ảnh";
                saveDialog.Filter = "Bitmap (*.bmp)|*.bmp";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ndPath = saveDialog.FileName;
                    if (ndPath == stPath)
                        MessageBox.Show("Không thể ghi đè lên hình ảnh đã chọn!", "Lỗi hệ thống");
                    else
                    {
                        try
                        {
                            CreateFile(stPath, ndPath, txtMessage.Text, password);
                            Bitmap bitmap = new Bitmap(ndPath);
                            FixPicture(bitmap, pictureAfter);
                            pictureAfter.Image = bitmap;
                            saveDialog.Dispose();
                            MessageBox.Show("Đã giấu tin thành công!", "Thông báo");
                        }
                        catch
                        {
                            MessageBox.Show("Ảnh này không phải bitmap hoặc đang được sử dụng!", "Lỗi hệ thống");
                        }
                    }
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            pictureBefore.Image = null;
            pictureAfter.Image = null;
            txtPath.Text = null;
            txtPassword.Text = null;
            txtConfirm.Text = null;
            txtMessage.Text = null;
        }

        private void btnChooseDec_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Chọn ảnh để giải mã";
            openFile.Filter = "Bitmap (*.bmp)|*.bmp";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                decPath = openFile.FileName;
                Bitmap bitmap = new Bitmap(decPath);
                FixPicture(bitmap, pictureBoxDec);
                txtPathDec.Text = decPath;
                txtResult.Text = "";
            }
        }

        private void btnDecode_Click(object sender, EventArgs e)
        {
            if (txtPasswordDec.Text != "" && txtPathDec.Text != "")
            {
                FileStream inStream = new FileStream(decPath, FileMode.Open, FileAccess.Read);
                inStream.Seek(offset, 0);
                byte[] byteLength = new byte[4];
                byteLength = Decode(inStream, 4);
                int length = BitConverter.ToInt32(byteLength, 0);
                inStream.Seek(offset + 4 * 8, 0);
                byte[] buffer = null;
                try
                {
                    buffer = new byte[length];
                    buffer = Decode(inStream, length);
                }
                catch
                {
                    MessageBox.Show("Ảnh này không chứa thông tin!", "Thông báo");
                    inStream.Close();
                    return;
                }
                byte[] hidenMessage = new byte[4 + buffer.Length];
                hidenMessage = ByteArray(byteLength, buffer);
                byte[] byteCheckPassword = new byte[32];
                byte[] byteMessage = new byte[length - 32];
                for (int i = 0; i < length; i++)
                {
                    if (i < 32)
                    {
                        byteCheckPassword[i] = hidenMessage[i + 4];
                    }
                    else
                    {
                        byteMessage[i - 32] = hidenMessage[i + 4];
                    }
                }
                UnicodeEncoding unicode = new UnicodeEncoding();
                String checkPassword = unicode.GetString(byteCheckPassword);
                String resultMess = unicode.GetString(byteMessage);
                String password = txtPasswordDec.Text;
                for (int i = password.Length; i < 16; i++)
                {
                    password += " ";
                }
                if (checkPassword != password)
                {
                    MessageBox.Show("Mật khẩu không đúng!", "Thông báo");
                }
                else
                {
                    txtResult.Text = resultMess;
                    MessageBox.Show("Giải mã thành công!", "Thông báo");
                }
                inStream.Close();
            }
            else
            {
                if (txtPathDec.Text == "")
                    MessageBox.Show("Vui lòng chọn ảnh để giải mã!", "Thông báo");
                else
                    MessageBox.Show("Vui lòng nhập mật khẩu để giải mã!", "Thông báo");
            }
        }
    }
}
