Imports Newtonsoft.Json
Imports System.Net
Public Class Github
    'Code is based on code found in HDT Compatibility Window
    Public Function CheckForUpdate(user As String, repo As String, version As Version) As GithubRelease
        Try
            Dim latest As GithubRelease = GetLatestRelease(user, repo)

            Dim v As Version = New Version(latest.tag_name)

            If v.CompareTo(version) > 0 Then
                Return latest
            End If
        Catch ex As Exception
            Return Nothing
        End Try
        Return Nothing
    End Function
    Public Function GetLatestRelease(user As String, repo As String) As GithubRelease
        Dim url = String.Format("https://api.github.com/repos/{0}/{1}/releases", user, repo)
        Try
            Dim json = ""
            Using wc As New WebClient
                wc.Headers.Add(HttpRequestHeader.UserAgent, user)
                json = wc.DownloadString(url)
            End Using

            Dim releases = JsonConvert.DeserializeObject(Of List(Of GithubRelease))(json)
            Return releases.FirstOrDefault()
        Catch ex As Exception
            Throw ex
        End Try
        Return Nothing
    End Function
    Public Class GithubRelease
        Public Property tag_name As String
    End Class
End Class
