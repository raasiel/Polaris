# Reads the xml configuration file (eg. sqa, staging)
# and then stores them in a hashtable.
function Fill-Config ($parent, $element)
{    
    foreach ($elm in $element)
    {
		$cvalue=$elm.InnerText
        if ($elm.FirstChild.NextSibling -eq $null)
        {
		
            if ($parent -is [System.Collections.ArrayList])
            {
                $parent.Add($cvalue) | Out-Null
            }
            elseif ( $parent -is [System.Collections.Hashtable] )
            {
                if ($elm.GetAttribute("type") -eq "xml")
    			{
    				$parent[$elm.Name]  = $elm;
    			}
                else
                {
                    $parent[$elm.Name]= $cvalue
                }
            }
        }
        else
        {   
			if ($elm.GetAttribute("type") -eq "list")
            {
                $child = New-Object System.Collections.ArrayList
                #$parent | Add-Member -membertype noteproperty -name $elm.Name -value $child
				$parent[$elm.Name] = $child
            }
			elseif ($elm.GetAttribute("type") -eq "xml")
			{
				$parent[$elm.Name]  = $elm;
			}			
            else
            {
                $child = New-Object System.Collections.Hashtable
                $parent[$elm.Name] =  $child
            }                                    
            Fill-Config $child $elm.ChildNodes
        }
    }
}

# Retrieves configuration parameter from a string
# for example [@root]\some folder will return root
function Get-ConfigExpression ($exp)
{
    if ($exp.Length -gt 0)
    {
        $start = $exp.IndexOf("[@")
        if ( $start -gt -1 )
        {
            $start = $stat + 2
            $end = $exp.IndexOf("]")
            $len = $end - $start
            return $exp.Substring($start, $len)
        }        
    }
    return ""
}

# Evalues a configuration parameter by reading the config 
# For example if root has value c:\build in the config file
# then [@root]\some folder will return c:\build\somefolder
function Get-ConfigValue ($conf, $exp)
{
	if ($exp.IndexOf("@") -eq -1)
	{
		return $exp;
	}
	
    $var = Get-ConfigExpression ($exp)
    $val = ""
    if ($var.Length -gt 0)
    {
        $val = $config[$var]
    }
	if ($val -is [System.Collections.ArrayList])
	{
		$a=1;
	}
	if ($val -is [string])
	{
		return $exp.Replace("[@$var]",$val)
	}
	else
	{
		return $val;		
	}
	
	
    
}

function Make-ArrayExpression ( $list )
{
	$sb = New-Object System.Text.StringBuilder
	$sb.Append("@(")  | Out-Null
	foreach($v in $list)
	{
		$sb.Append("`"")  | Out-Null
		$sb.Append($v)  | Out-Null
		$sb.Append("`",")  | Out-Null
	}
	$sb.Remove($sb.Length-1,1) | Out-Null
	$sb.Append(") ")  | Out-Null
	return $sb.toString() 
}

# Core engine.
# Runs the steps for specified product for specified configuration
function Run-Steps 
{ 
	param
	(
		[int]$start_step, 		# which step to start from
		[int]$end_step,			# which step to end to
		[string]$steps_name,	# name of the steps file inside the product\steps folder. Eg. steps
		[string]$config_name,	# name of config file inside the product\config folder. Eq. sqa
		[string]$product
	)

	# Evaluate location
	$config_location = ".\products\$product\config\$config_name.xml"
	$steps_location = ".\products\$product\steps\$steps_name.xml"

	# Load the steps
	$xml_steps = [xml](Get-Content $steps_location)
	$xml_config = [xml](get-content $config_location)

	# Build the configuration object
	$config = $null
	$config = New-Object System.Collections.Hashtable

	# Load configuration object
	Fill-Config $config $xml_config.DocumentElement.FirstChild.ChildNodes

	# Loop through the steps. Check if we can execute the steps
	# then prepare the command string and then make it into a 
	# a scriptblock and then execute the script block
	foreach ($step in $xml_steps.DocumentElement.ChildNodes)
	{   
		$current_step = [Int]::Parse($step.id)
		if ($current_step -ge $start_step)
		{
			if ($current_step -le $end_step)
			{			
				$sb = new-Object System.Text.StringBuilder
				$commandName = $step.FirstChild.Name
				$sb.Append($commandName) 			| Out-Null
			    foreach ($attr in $step.FirstChild.Attributes)
			    {
					$val = Get-ConfigValue $config $attr.Value
					$sb.Append(" -parm_") 			| Out-Null
					$sb.Append($attr.Name)    		| Out-Null
					$sb.Append(" ") 				| Out-Null
					if ($val -is [System.Object[]])
					{
						$exp = Make-ArrayExpression $val
						$sb.Append($exp) | Out-Null
					}
					elseif($val -is [System.Xml.XmlElement])
					{
						Write-Host "this is xml"
						$sb.Append("$val")    			| Out-Null
					}
					else
					{
						$sb.Append("`"$val`"")    			| Out-Null
					}
			    }		
				Write-Host "`n============================================================"
				Write-Host "Will execute step $current_step . Command is $commandName ."
				$commandText = $sb.ToString()
				# Create context
				$func_context = @{"config" = $config; "func_name" = $commandName;}

				$scr = $scriptBlock = [Scriptblock]::Create($commandText)
				Write-Host "Executing ' $commandText '"
				Write-Host "------------------------------------------------------------"
				Write-Host ""
				try
				{
					Invoke-Command -ScriptBlock $scr 
					Write-Host "Step $current_step : $commandName is completed. `n"
				}
				catch 
				{
					Write-Host "Step $current_step : $commandName has failed. `n" -ForegroundColor Red
					Write-Host $error[0] -ForegroundColor Red			
				}
				finally
				{
					$function_context = $null
				}
				
			}
		}
	}		
}


function Execute-DeployTool 
{
	param
	(
		[string] $action,
		[System.Collections.Hashtable] $paramters
	)
	
	$sb = new-Object System.Text.StringBuilder	
	$sb.Append(".\tools\DeploymentUtility.exe $action") | Out-Null
	foreach($key in $paramters.Keys)
	{
		$sb.Append(" /")								| Out-Null
		$sb.Append($key)								| Out-Null
		$sb.Append(":")									| Out-Null
		$sb.Append($paramters[$key])					| Out-Null
	}
	
	$scr = $scriptBlock = [Scriptblock]::Create($sb.ToString())
	Invoke-Command -ScriptBlock $scr 

}