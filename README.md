# What is Docx2Pdf
Its a simple webservice converting docx files to pdf.

## Usage
### Build docker image
`docker build -t docx2pdf .`
### Run docker
`docker run -d --name docx2pdf -p 80:80 docx2pdf`
### Convert docx to pdf
`curl --form file=@file2convert.docx http://localhost/convert -o output_file.pdf`

## Install on kubernetes cluster
`kubectl apply -f docx2pdf.yaml`