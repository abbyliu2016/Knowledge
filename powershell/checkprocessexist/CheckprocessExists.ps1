Param(
   #  [Parameter(Mandatory=$True)]
   #  [int] $start,
   #  [Parameter(Mandatory=$True)]
   #  [int] $end
)

$processName = "TestBulkInsert11"
$errorLog = "$targetDrive\UninstallXpertAgentError.LOG"

    $count = 0;
    $timeoutCount = 20;
    $flag = $False

    $num = 39;

    while($num -le 50)
    {
        if((get-process $processName -ea SilentlyContinue) -eq $Null)
        { 
           echo "$processName is not running" 
          
           $start = $num
           $end = $num + 1
         
           Start-Process -FilePath "D:\src\Knowledge\powershell\checkprocessexist\Release\TestBulkInsert11.exe" -ArgumentList $start, $end
           $num++;
        }
        else
        { 
         echo "$processName is running" 
            Start-Sleep -s 600
        }
   }

    
