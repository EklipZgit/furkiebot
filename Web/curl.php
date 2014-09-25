<?php
// cURL replacement for file_get_contents()
// Safer alternative to enabling allow_url_fopen in php.ini :)

function file_get_contents_curl($url) {
  $ch = curl_init();
  curl_setopt($ch, CURLOPT_HEADER, 0);
  curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
  curl_setopt($ch, CURLOPT_URL, $url);
  $data = curl_exec($ch);
  curl_close($ch);
echo print_r($data);
echo "<br>"; 
  return $data;
}
?>