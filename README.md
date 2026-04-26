Per installare => sc create MyWorkerService binPath= "C:\MyServices\bin\Release\net8.0\publish\win-x86\WorkerService.exe"

FLUSSO

chiamata addfile a webapi
webapi notifica client che ha GUID da scaricare
client scrive su channel 
semaforo controllo se ci sono slot liberi
client chiama webapi per recuperare file tramite GUID
webapi cancella riga su db

NOTE

semaforo serve per limitare task in parallelo 
channel serve per mettere in coda le chiamate e limitare i nuovi task creati.

PROBLEMI

se quando arriva chiamata a webapi e client è spento oppure succede qualcosa riga rimane su db evento perso va rifatta richiesta al server
se client chiama webapi per recuperare file e succede qualcosa , si perde file ,riga rimane su db evento perso va rifatta richiesta al server
