$base = 'C:\Program Files (x86)\Steam\steamapps\common\Darkwood\Darkwood_Data\Managed'
Get-ChildItem $base -Filter *.dll | ForEach-Object { try { [void][System.Reflection.Assembly]::LoadFile($_.FullName) } catch {} }
$asm = [System.Reflection.Assembly]::LoadFile((Join-Path $base 'Assembly-CSharp.dll'))
$module = $asm.ManifestModule
$t = $asm.GetType('CharacterEffect')
$m = $t.GetMethod('Update', [System.Reflection.BindingFlags] 'Public,NonPublic,Instance,Static,DeclaredOnly')
$body = $m.GetMethodBody()
$bytes = $body.GetILAsByteArray()
$single = New-Object 'System.Collections.Generic.Dictionary[byte,object]'
$double = New-Object 'System.Collections.Generic.Dictionary[System.UInt16,object]'
[System.Reflection.Emit.OpCodes].GetFields([System.Reflection.BindingFlags] 'Public,Static') | ForEach-Object {
    $op = $_.GetValue($null)
    if($op.Size -eq 1){ $single[[byte]$op.Value] = $op } else { $double[[System.UInt16]$op.Value] = $op }
}
for($i = 0; $i -lt $bytes.Length; $i++) {
    $offset = $i
    $code = $bytes[$i]
    if($code -eq 0xFE) {
        $i++
        $code = [System.UInt16](0xFE00 -bor $bytes[$i])
        $op = $double[$code]
    } else {
        $op = $single[[byte]$code]
    }
    $i++
    $operand = ''
    switch($op.OperandType) {
        'ShortInlineI' { $operand = [sbyte]$bytes[$i]; $i += 1 }
        'InlineI' { $operand = [BitConverter]::ToInt32($bytes,$i); $i += 4 }
        'InlineMethod' { $token = [BitConverter]::ToInt32($bytes,$i); $res = $module.ResolveMethod($token); $operand = $res.DeclaringType.FullName + '::' + $res.Name; $i += 4 }
        'InlineField' { $token = [BitConverter]::ToInt32($bytes,$i); $f = $module.ResolveField($token); $operand = $f.DeclaringType.FullName + '::' + $f.Name; $i += 4 }
        'InlineType' { $token = [BitConverter]::ToInt32($bytes,$i); $operand = $module.ResolveType($token).FullName; $i += 4 }
        'ShortInlineBrTarget' { $operand = [sbyte]$bytes[$i]; $i += 1 }
        'InlineBrTarget' { $operand = [BitConverter]::ToInt32($bytes,$i); $i += 4 }
        'InlineString' { $token = [BitConverter]::ToInt32($bytes,$i); $operand = $module.ResolveString($token); $i += 4 }
        'ShortInlineVar' { $operand = $bytes[$i]; $i += 1 }
        'InlineVar' { $operand = [BitConverter]::ToUInt16($bytes,$i); $i += 2 }
        'InlineSwitch' { $count = [BitConverter]::ToInt32($bytes,$i); $i += 4 + (4*$count) }
        'InlineI8' { $i += 8 }
        'ShortInlineR' { $operand = [BitConverter]::ToSingle($bytes,$i); $i += 4 }
        'InlineR' { $operand = [BitConverter]::ToDouble($bytes,$i); $i += 8 }
        'InlineSig' { $i += 4 }
        'InlineTok' { $i += 4 }
    }
    $line = if($operand -ne '') { '{0:D4}: {1} {2}' -f $offset, $op.Name, $operand } else { '{0:D4}: {1}' -f $offset, $op.Name }
    if($line -match 'poison|bleed|gas|hunger|Health|maxHealth|modifier|duration|interval|set_health|damage|heal|call') {
        $line
    }
}
