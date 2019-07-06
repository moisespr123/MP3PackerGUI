Public Class Form1

    Private Sub InputBrowseBtn_Click(sender As Object, e As EventArgs) Handles InputBrowseBtn.Click
        Dim InputBrowser As New FolderBrowserDialog With {
            .ShowNewFolderButton = False
        }
        Dim OkAction As MsgBoxResult = InputBrowser.ShowDialog
        If OkAction = MsgBoxResult.Ok Then
            InputTxt.Text = InputBrowser.SelectedPath
        End If
    End Sub

    Private Sub OutputBrowseBtn_Click(sender As Object, e As EventArgs) Handles OutputBrowseBtn.Click
        Dim OutputBrowser As New FolderBrowserDialog With {
            .ShowNewFolderButton = True
        }
        Dim OkAction As MsgBoxResult = OutputBrowser.ShowDialog
        If OkAction = MsgBoxResult.Ok Then
            OutputTxt.Text = OutputBrowser.SelectedPath
        End If
    End Sub

    Private Sub StartBtn_Click(sender As Object, e As EventArgs) Handles StartBtn.Click
        StartBtn.Enabled = False
        InputTxt.Enabled = False
        OutputTxt.Enabled = False
        InputBrowseBtn.Enabled = False
        OutputBrowseBtn.Enabled = False
        enableMultithreading.Enabled = False
        Dim StartTasks As New Threading.Thread(Sub() StartThreads())
        StartTasks.Start()
    End Sub
    Private Sub StartThreads()
        If Not String.IsNullOrEmpty(OutputTxt.Text) Then If Not My.Computer.FileSystem.DirectoryExists(OutputTxt.Text) Then My.Computer.FileSystem.CreateDirectory(OutputTxt.Text)
        Dim ItemsToProcess As List(Of String) = New List(Of String)
        Dim IgnoreFilesWithExtensions As String = String.Empty
        If My.Computer.FileSystem.FileExists("ignore.txt") Then IgnoreFilesWithExtensions = My.Computer.FileSystem.ReadAllText("ignore.txt")
        For Each File In IO.Directory.GetFiles(InputTxt.Text)
            If IO.Path.GetExtension(File) = ".mp3" Then
                ItemsToProcess.Add(File)
            End If
        Next
        ProgressBar1.BeginInvoke(Sub()
                                     ProgressBar1.Maximum = ItemsToProcess.Count
                                     ProgressBar1.Value = 0
                                 End Sub
        )
        Dim tasks = New List(Of Action)
        Dim outputPath As String = ""
        If enableMultithreading.Checked Then
            For Counter As Integer = 0 To ItemsToProcess.Count - 1
                If Not String.IsNullOrEmpty(OutputTxt.Text) Then
                    outputPath = OutputTxt.Text + "\" + IO.Path.GetFileName(ItemsToProcess(Counter))
                End If
                Dim args As Array = {ItemsToProcess(Counter), outputPath}
                tasks.Add(Function() run_mp3packer(args))
            Next
            Parallel.Invoke(New ParallelOptions With {.MaxDegreeOfParallelism = Environment.ProcessorCount}, tasks.ToArray())
        Else
            For Counter As Integer = 0 To ItemsToProcess.Count - 1
                If Not String.IsNullOrEmpty(OutputTxt.Text) Then
                    outputPath = OutputTxt.Text + "\" + IO.Path.GetFileName(ItemsToProcess(Counter))
                End If
                Dim args As Array = {ItemsToProcess(Counter), outputPath}
                run_mp3packer(args)
            Next
        End If
        StartBtn.BeginInvoke(Sub()
                                 StartBtn.Enabled = True
                                 enableMultithreading.Enabled = True
                                 InputTxt.Enabled = True
                                 OutputTxt.Enabled = True
                                 InputBrowseBtn.Enabled = True
                                 OutputBrowseBtn.Enabled = True
                             End Sub)
        MsgBox("Finished")
    End Sub
    Private Function run_mp3packer(args As Array)
        Dim Input_File As String = args(0)
        Dim Output_File As String = args(1)
        Dim ffmpegProcessInfo As New ProcessStartInfo
        Dim ffmpegProcess As Process
        ffmpegProcessInfo.FileName = "mp3packer.exe"
        ffmpegProcessInfo.Arguments = "-z -R --nice 0 """ + Input_File + """ """ + Output_File + """"
        ffmpegProcessInfo.CreateNoWindow = True
        ffmpegProcessInfo.RedirectStandardOutput = False
        ffmpegProcessInfo.UseShellExecute = False
        ffmpegProcess = Process.Start(ffmpegProcessInfo)
        ffmpegProcess.WaitForExit()
        ProgressBar1.BeginInvoke(Sub() ProgressBar1.PerformStep())
        Return True
    End Function

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        enableMultithreading.Checked = My.Settings.Multithreading
        If Not mp3packerExist() Then
            MessageBox.Show("mp3packer.exe was not found. Exiting...")
            Me.Close()
        End If
    End Sub


    Private Function mp3packerExist() As Boolean
        If My.Computer.FileSystem.FileExists("mp3packer.exe") Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub EnableMultithreading_CheckedChanged(sender As Object, e As EventArgs) Handles enableMultithreading.CheckedChanged
        My.Settings.Multithreading = enableMultithreading.Checked
        My.Settings.Save()
    End Sub

    Private Sub Form1_DragDrop(sender As Object, e As DragEventArgs) Handles MyBase.DragDrop
        If IO.Directory.Exists(CType(e.Data.GetData(DataFormats.FileDrop), String())(0)) Then InputTxt.Text = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
    End Sub

    Private Sub Form1_DragEnter(sender As Object, e As DragEventArgs) Handles MyBase.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub
End Class
