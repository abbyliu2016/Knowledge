Param(
     [switch] $register,
     [Parameter(Mandatory=$True)]
     [string] $targetDrive
)

$taskName = "UninstallXpertAgent"
$psFile = Join-Path -Path $targetDrive -ChildPath "UninstallXpertAgent.ps1";
$errorLog = "$targetDrive\UninstallXpertAgentError.LOG"

if($register) 
{
    $appDir = "$targetDrive\XpertAgent\app"
    $configFile = "$appDir\XpertAgent\UninstallXpertAgent.xml"
    $xpertAgentStarterTaskScript = Get-Content $configFile
    $xpertAgentStarterTaskScript | % { $_.Replace("[TargetDrive]", "$targetDrive")} | Set-Content $configFile

    Schtasks /Create /XML $configFile /TN $taskName /RU System /f

    $ERROR | Out-File $errorLog
}
else
{
    $count = 0;
    $timeoutCount = 20;
    $flag = $False

    while($True)
    {
        if((get-process "XpertAgent.Proxy" -ea SilentlyContinue) -eq $Null)
        { 
           echo "XpertAgent.Proxy is not running" 
           $count++;

           if ($count -eq $timeoutCount)
           {
               $flag = $True
               break;
           }

           Start-Sleep -s 4
        }
        else
        { 
            break;
        }
   }

    if ($flag -eq $True)
    {
        $TaskNames = @( "XpertAgent", "XpertAgentStarter" )
        for ($i = 0; $i -lt $TaskNames.Length; $i++)
        {
            Unregister-ScheduledTask -TaskName  $TaskNames[$i] -Confirm:$false -EA SilentlyContinue
        }
        
        $p = Get-Process "Xpert.Agent.DataLauncher" -EA SilentlyContinue
        Stop-Process -Name "Xpert.Agent" -Force -EA SilentlyContinue
        Wait-Process -Name "Xpert.Agent" -EA SilentlyContinue
        Wait-Process -id $p.id -EA SilentlyContinue
        
        $folderName = $targetDrive + "\XpertAgent"
        for ($j = 0; $j -lt 3; $j++)
        {
            Remove-Item $folderName -Recurse -Force -EA SilentlyContinue
            if (Test-Path $folderName -eq $False)
            {
                break;
            }

            Start-Sleep -Milliseconds 100
        }

        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -EA SilentlyContinue

        Remove-Item $psFile -Force -EA SilentlyContinue

        $ERROR | Out-File "$targetDrive\UninstallXpertAgentError.LOG"
    }
}
