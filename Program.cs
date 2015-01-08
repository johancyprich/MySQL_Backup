/// APPLICATION: MySQL Backup
/// VERSION: 1.1.0
/// DATE: October 8, 2013
/// AUTHOR: Johan Cyprich
/// AUTHOR EMAIL: jcyprich@live.com
/// 
/// LICENSE:
/// The MIT License (MIT)
///
/// Copyright (c) 2013 Johan Cyprich. All rights reserved.
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy 
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
///
/// SUMMARY:
/// Backs up selected databases to a folder.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace MySQL_Backup
{
  class Program
  {
    enum ErrorCode : int
    {
      Success = 0,
      Warning = 1,
      Failed = 2,
      DatabaseNotFound = 100,
      DriveFull = 101,
      UnknownError = 255
    }

    static ErrorCode error;
    static int exitCode = 0;

    static string appPath;                              // path to mysqldump.exe
    static string backupPath;                           // path to backup .sql files to
    static string logPath;                              // path to log file

    static string currentDate;                          // current date and time

    static string mySQLserver;                          // MySQL host name
    static string mySQLuserName;                        // MySQL user name
    static string mySQLpassword;                        // MySQL password

    static string SMTPserver;                           // host name of mail server
    static string SMTPuserName;                         // user name for e-mail account
    static string SMTPpassword;                         // password for e-mail account
    static string mailRecipient;                        // account that receives the e-mail

    static string [] dbs;                               // list of databases to backup


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// SUMMARY:
    /// Initialize global variables.
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static Program ()
    {
      error = ErrorCode.Success;

      // Set paths for MySQL and backup location.

      appPath = @"C:\Program Files\MySQL\mysql-5.6.13-win32\bin\mysqldump.exe";
      backupPath = @"D:\Backups\Backup ";
      logPath = @"D:\Backups\";

      // Set parameters for the MySQL user.

      mySQLserver = "dbserver";
      mySQLuserName = "dbuser";
      mySQLpassword = "dbpassword";

      // Set parameters for the mail server.

      SMTPserver = "mail.mailserver.com";
      SMTPuserName = "user@mailserver.com";
      SMTPpassword = "abc123";
      mailRecipient = "recipient@mailserver.com";

      // Set databases to be backup as a string array.

      dbs = new string []
            {
              "db1",
              "db2",
			 	      "db3"
            };

      // Create the log file.

      currentDate = DateTime.Now.ToString ("yyyyMMdd_HHmmss");
      logPath += "Backup " + currentDate + ".txt";

      using (StreamWriter log = File.CreateText (logPath))
      {
        log.WriteLine ("MySQL_Backup");
        log.WriteLine ("============");
        log.WriteLine ("");
      }
    } // static Program ()


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// SUMMARY:
    /// Main program.
    ///
    /// PARAMETERS:
    /// args: Not used.
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void Main (string [] args)
    {
      Console.WriteLine ("MySQL_Backup");
      Console.WriteLine ("============");
      Console.WriteLine ("");

      CreateBackupFolder ();

      foreach (var db in dbs)
        BackupDatabase (db);

      EmailLog ();

      Console.WriteLine ("");
      Console.WriteLine ("Backups complete.");

      Environment.Exit (exitCode);
    } // static void Main (string [] args)


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// SUMMARY
    /// Write name and size of backup file to log file.
    ///
    /// PARAMETERS:
    /// db (in): Backed up database.
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void AppendLog (string db, int result)
    {
      using (StreamWriter log = File.AppendText (logPath))
      {
        FileInfo f = new FileInfo (backupPath + db + ".sql");        // file handle to database backup
        long filesize = f.Length;                                    // size of backup file

        if (result != 0)
          exitCode = 1;

        switch (result)
        {
          case 0: 
            error = ErrorCode.Success;
            log.WriteLine (db + " (" + Convert.ToString (filesize) + " bytes)");
            break;

          case 1: 
            error = ErrorCode.Warning;
            log.WriteLine (db + " (" + Convert.ToString (filesize) + " bytes) ... WARNING");
            break;

          case 2: 
            error = ErrorCode.DatabaseNotFound;
            log.WriteLine (db + " (" + Convert.ToString (filesize) + " bytes) ... DATABASE NOT FOUND");
            break;
          
          default: 
            error = ErrorCode.UnknownError;
            log.WriteLine (db + " (" + Convert.ToString (filesize) + " bytes) ... UNKNOWN ERROR");
            break;
       } 

        log.Close ();
      }
    } // void AppendLog (string sDatabase)


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// SUMMARY:
    /// Backup a database as a .SQL file to a folder.
    /// 
    /// PARAMETERS:
    /// db (in): Database to backup.
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void BackupDatabase (string db)
    {
      Console.WriteLine ("Backing up {0}.", db);

      FileStream StreamDB = new FileStream (backupPath + db + ".sql", FileMode.Create, FileAccess.Write);

      using (StreamWriter SW = new StreamWriter (StreamDB))
      {
        ProcessStartInfo proc = new ProcessStartInfo ();

        proc.FileName = appPath;

        proc.RedirectStandardInput = false;
        proc.RedirectStandardOutput = true;
        proc.UseShellExecute = false;
        proc.WindowStyle = ProcessWindowStyle.Minimized;
        proc.Arguments = " --host=" + mySQLserver + " --user=" + mySQLuserName + " --password=" + mySQLpassword
                       + " --default-character-set=utf8 " + db;
        proc.CreateNoWindow = true;

        Process p = Process.Start (proc);

        SW.Write (p.StandardOutput.ReadToEnd ());
        p.WaitForExit ();

        AppendLog (db, p.ExitCode);

        p.Close ();
        SW.Close ();
        StreamDB.Close ();
      }
    } // static void BackupDatabase (string db)


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// SUMMARY:
    /// Creates a folder to write backup files to.
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void CreateBackupFolder ()
    {
      backupPath += currentDate + "\\";

      Directory.CreateDirectory (backupPath);
    } // static void CreateBackupFolder ()


    ///////////////////////////////////////////////////////////////////////////////////////////////
    /// SUMMARY:
    /// E-mails the log file.
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void EmailLog ()
    {
      using (MailMessage Email = new MailMessage ())
      {
        string subject = "";

        // Copy backup log into string.

        System.IO.StreamReader DatabaseBackup = new System.IO.StreamReader (logPath);
        string sDatabaseBackup = DatabaseBackup.ReadToEnd ();

        // Build e-mail message.

        Email.IsBodyHtml = true;

        Email.From = new MailAddress (SMTPuserName);
        Email.To.Add (mailRecipient);

        if (exitCode == 0)
          subject = "Backup MySQL Log: " + currentDate;
          
        else
          subject = "*** ERRORS: Backup MySQL Log *** on " + currentDate;

        Email.Subject = subject;
        Email.Body = "<pre>" + sDatabaseBackup + "</pre>"
                   + @"<p>Files backed up to <b>" + backupPath + "</b></p>"
                   + @"<p>Log file at <b>" + logPath + "</b></p>";

        SmtpClient smtp = new SmtpClient (SMTPserver);

        // Use SMTP authentication if username is supplied.

        System.Net.NetworkCredential SMTPUserInfo = new System.Net.NetworkCredential (SMTPuserName, SMTPpassword);

        smtp.UseDefaultCredentials = false;
        smtp.Credentials = SMTPUserInfo;

        try
        {
          // Send the mail.

          smtp.Send (Email);
        } // try

        catch (Exception ex)
        {
          // ignore error message
        } // catch (Exception e)
      } // using (MailMessage Email = new MailMessage ())
    } // private void EmailLog ()

  } // class Program
} // namespace MySQL_Backup
