using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Extensions;

namespace ImageMagickConverterLauncher
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    string imageMagickPathKey = Registry.CurrentUser.ToString() + @"\Software\ImageMagickConverterLauncher";
    string imageMagickPathValue = "ConvertPath";
    string strImageMagickPath = @"C:\Program Files\ImageMagick-6.9.1-Q16";
    const string strImageMagickConvertFileName = "convert.exe";
    const string strDefaultDestinationImageExt = "jpg";

    public MainWindow()
    {
      InitializeComponent();
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void Convert(string source, string destination, bool playSound)
    {
      try
      {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        psi.FileName = Path.Combine(strImageMagickPath, strImageMagickConvertFileName);
        psi.Arguments = "\"" + source + "\"" + " " + "\"" + destination + "\"";

        psi.WorkingDirectory = strImageMagickPath;

        btnSourceImageOpen.SetPropertyThreadSafe(() => btnSourceImageOpen.IsEnabled, false);
        btnDestinationImageOpen.SetPropertyThreadSafe(() => btnDestinationImageOpen.IsEnabled, false);
        btnConvert.SetPropertyThreadSafe(() => btnConvert.IsEnabled, false);
        btnClose.SetPropertyThreadSafe(() => btnClose.IsEnabled, false);
        lblDone.SetPropertyThreadSafe(() => lblDone.Visibility, Visibility.Hidden);
        barConverting.SetPropertyThreadSafe(() => barConverting.Visibility, Visibility.Visible);
        using (Process pr = Process.Start(psi))
        {
          pr.WaitForExit();
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
      finally
      {
        if (playSound)
        {
          SystemSounds.Beep.Play();
        }
        btnSourceImageOpen.SetPropertyThreadSafe(() => btnSourceImageOpen.IsEnabled, true);
        btnDestinationImageOpen.SetPropertyThreadSafe(() => btnDestinationImageOpen.IsEnabled, true);
        btnConvert.SetPropertyThreadSafe(() => btnConvert.IsEnabled, true);
        btnClose.SetPropertyThreadSafe(() => btnClose.IsEnabled, true);
        lblDone.SetPropertyThreadSafe(() => lblDone.Visibility, Visibility.Visible);
        barConverting.SetPropertyThreadSafe(() => barConverting.Visibility, Visibility.Hidden);
      }
    }

    private void btnConvert_Click(object sender, RoutedEventArgs e)
    {
      if (edtSourceImage.Text == string.Empty)
      {
        MessageBox.Show("Source file path is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      if (edtDestinationImage.Text == string.Empty)
      {
        MessageBox.Show("Destination file path is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      string source = edtSourceImage.Text;
      string destination = edtDestinationImage.Text;

      Task.Factory.StartNew(() =>
      {
        Convert(source, destination, true);
      });
    }

    private void AutoSetImageFields(string sourceName)
    {
      edtSourceImage.Text = sourceName;
      string path = Path.GetDirectoryName(edtSourceImage.Text);
      string name = Path.GetFileNameWithoutExtension(edtSourceImage.Text);
      if (edtDestinationImage.Text == string.Empty)
      {
        edtDestinationImage.Text = Path.Combine(path, name + "." + strDefaultDestinationImageExt);
      }
      else
      {
        edtDestinationImage.Text = Path.Combine(Path.GetDirectoryName(edtDestinationImage.Text), name + "." + Path.GetExtension(edtDestinationImage.Text));
      }
    }

    private void btnSourceImageOpen_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      if (edtSourceImage.Text != string.Empty)
      {
        ofd.InitialDirectory = Path.GetDirectoryName(edtSourceImage.Text);
        ofd.FileName = edtSourceImage.Text;
      }
      if (ofd.ShowDialog() == true)
      { 
        AutoSetImageFields(ofd.FileName);
      }
    }

    private void btnDestinationImageOpen_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog sfd = new SaveFileDialog();
      if (edtDestinationImage.Text != string.Empty)
      {
        sfd.InitialDirectory = Path.GetDirectoryName(edtDestinationImage.Text);
        sfd.DefaultExt = Path.GetExtension(edtDestinationImage.Text);
      }
      else
      {
        if (edtSourceImage.Text != string.Empty)
        {
          sfd.InitialDirectory = Path.GetDirectoryName(edtSourceImage.Text);
        }
        sfd.DefaultExt = strDefaultDestinationImageExt;
      }
      if (edtSourceImage.Text != string.Empty)
      {
        if (edtDestinationImage.Text != string.Empty)
        {
          sfd.FileName = Path.Combine(Path.GetDirectoryName(edtDestinationImage.Text), Path.GetFileNameWithoutExtension(edtSourceImage.Text) + 
            "." + Path.GetExtension(edtDestinationImage.Text));
        }
        else
        {
          sfd.FileName = Path.Combine(Path.GetDirectoryName(edtSourceImage.Text), Path.GetFileNameWithoutExtension(edtSourceImage.Text) +
            "." + strDefaultDestinationImageExt);
        }
      }
      if (sfd.ShowDialog() == true)
      {
        edtDestinationImage.Text = sfd.FileName;
      }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
      string[] droppedFiles = null;
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        droppedFiles = e.Data.GetData(DataFormats.FileDrop, true) as string[];
      }

      if ((null == droppedFiles) || (!droppedFiles.Any())) { return; }
      foreach (var file in droppedFiles)
      {
        AutoSetImageFields(file);
        Convert(edtSourceImage.Text, edtDestinationImage.Text, false);
      }
      SystemSounds.Beep.Play();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      var value = Registry.GetValue(imageMagickPathKey, imageMagickPathValue, string.Empty);
      strImageMagickPath = value == null ? string.Empty : value as string;
      if (strImageMagickPath == string.Empty || !File.Exists(Path.Combine(strImageMagickPath, strImageMagickConvertFileName)))
      {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.FileName = strImageMagickConvertFileName;

        if (ofd.ShowDialog() != true || Path.GetFileName(ofd.FileName) != strImageMagickConvertFileName)
        {
          MessageBox.Show("Correct ImageMagick path should be defined", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          Close();
        }
        else
        {
          strImageMagickPath = Path.GetDirectoryName(ofd.FileName);
        }
      }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Registry.SetValue(imageMagickPathKey, imageMagickPathValue, strImageMagickPath);
    }
  }
}
