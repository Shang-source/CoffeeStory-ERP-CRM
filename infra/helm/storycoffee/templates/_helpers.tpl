{{- define "storycoffee.secretName" -}}
{{- if .Values.secret.existingName -}}
{{- .Values.secret.existingName -}}
{{- else -}}
{{- .Values.secret.name | default "storycoffee-secret" -}}
{{- end -}}
{{- end -}}
