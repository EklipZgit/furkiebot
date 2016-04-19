<?php
    function _substr($data, $pos, $len) {
        if (0 <= $pos && $pos + $len <= strlen($data) && $pos <= $pos + $len) {
            return substr($data, $pos, $len);
        }
        return null;
    }

    function _unpack_byte($data, $pos) {
        $str = _substr($data, $pos, 1);
        if ($str === null) {
            return null;
        }
        return first(unpack("C", $str));
    }

    function _unpack_le2($data, $pos) {
        $str = _substr($data, $pos, 2);
        if ($str === null) {
            return null;
        }
        return first(unpack("s", $str));
    }

    function _unpack_le4($data, $pos) {
        $str = _substr($data, $pos, 4);
        if ($str === null) {
            return null;
        }
        return first(unpack("l", $str));
    }

    function _pack_byte($data) {
        return pack("C", $data);
    }

    function _pack_le2($data) {
        return pack("s", $data);
    }

    function _pack_le4($data) {
        return pack("l", $data);
    }

    function _eat_bits_le($data, $pos, $num) {
        $val = 0;
        for ($i = 0; $i < $num; $i++) {
            $off = $pos + $i;
            if (_unpack_byte($data, (int)($off / 8)) & (1 << $off % 8)) {
                $val |= 1 << $i;
            }
        }
        return $val;
    }

    function _write_bits_le(&$data, $pos, $num, $val) {
        for ($i = 0; $i < $num; $i++) {
            $off = $pos + $i;
            $bin = (int)($off / 8);
            $bit = $off % 8;
            $bt = _unpack_byte($data, $bin);
            if ($val & 1 << $i) {
                $bt |= 1 << $bit;
            } else {
                $bt &= ~(1 << $bit);
            }
            $data[$bin] = _pack_byte($bt);
        }
    }

    function parse_replay($replay) {
        if (_substr($replay, 0, 7) != "DF_RPL1") {
            return null;
        }

        $header = array(
            'unk' => _unpack_le2($replay, 7),
            'uncompressedSize' => _unpack_le4($replay, 9),
            'frames' => _unpack_le4($replay, 13),
            'character' => _unpack_byte($replay, 17)
          );
        $level_len = _unpack_byte($replay, 18);
        $header['level'] = _substr($replay, 19, $level_len);

        if ($header['level'] === null || 19 + $level_len > strlen($replay)) {
            return null;
        }

        $replay = @gzuncompress(substr($replay, 19 + $level_len));
        if ($replay === false) {
            return null;
        }

        $pos = 0;
        $inputsLen = _unpack_le4($replay, $pos); $pos += 4;
        $inputs = array();

        for ($i = 0; $i < 7; $i++) {
            $len = _unpack_le4($replay, $pos); $pos += 4;
            $datum = _substr($replay, $pos, $len); $pos += $len;
            if ($datum === null) {
                return null;
            }

            $state = 0;
            $states = "";
            $payload_size = $i < 5 ? 2 : 4;
            for ($dpos = 0; $dpos + 8 <= 8 * strlen($datum);
                      $dpos += 8 + $payload_size) {
                $res = _eat_bits_le($datum, $dpos, 8);
                if ($res == 0xFF) {
                    break;
                }
                for ($j = ($dpos == 0 ? 1 : 0); $j <= $res; $j++) {
                    $states .= dechex($state);
                }
                if ($dpos + 8 + $payload_size <= 8 * strlen($datum)) {
                    $state = _eat_bits_le($datum, $dpos + 8, $payload_size);
                }
            }
            $inputs[] = $states;
        }

        $entityFrameContainers = array();
        $entityFrameContainerCount = _unpack_le4($replay, $pos); $pos += 4;
        if ($entityFrameContainerCount === null) {
            return null;
        }
        for ($i = 0; $i < $entityFrameContainerCount; $i++) {
            $entityFrameCount = _unpack_le4($replay, $pos + 8);
            $entityFrameContainer = array(
                'unk1' => _unpack_le4($replay, $pos),
                'unk2' => _unpack_le4($replay, $pos + 4),
                'entityFrames' => array(),
              );
            /* Ordered by unk1. */
            /* unk2 makes a permutation of [1, Count]. */
            $pos += 12;

            for ($j = 0; $j < $entityFrameCount; $j++) {
                $entityFrameContainer['entityFrames'][] = array(
                    'time' => _unpack_le4($replay, $pos),
                    'xpos' => _unpack_le4($replay, $pos + 4),
                    'ypos' => _unpack_le4($replay, $pos + 8),
                    'xspeed' => _unpack_le4($replay, $pos + 12),
                    'yspeed' => _unpack_le4($replay, $pos + 16)
                  );
                $pos += 20;
            }
            $entityFrameContainers[] = $entityFrameContainer;
        }
        if ($pos != strlen($replay)) {
            return null;
        }

        return array(
            'header' => $header,
            'inputs' => $inputs,
            'entityFrameContainers' => $entityFrameContainers
          );
    }

    function build_replay($repmeta) {
        $input_data = "";
        for ($i = 0; $i < 7; $i++) {
            $parts = array();
            $input = $repmeta['inputs'][$i];
            for ($j = 0; $j < strlen($input); ) {
                $sz = 1;
                while ($sz < 100 && $j + $sz < strlen($input) &&
                            $input[$j] == $input[$j + $sz]) {
                    $sz++;
                }
                $parts[] = array($sz, hexdec($input[$j]));
                $j += $sz;
            }

            $payload_size = $i < 5 ? 2 : 4;
            $bits = 8 + (8 + $payload_size) * count($parts);

            $input = "";
            while (strlen($input) * 8 < $bits) {
                $input .= "\xff";
            }
            $input .= "\xff\xff";

            _write_bits_le($input, 0, 8, 0);
            $pos = 8;
            foreach ($parts as $part) {
                _write_bits_le($input, $pos, $payload_size, $part[1]);
                $pos += $payload_size;
                _write_bits_le($input, $pos, 8, $part[0] - 1);
                $pos += 8;
            }

            $input_data .= _pack_le4(strlen($input));
            $input_data .= $input;
        }

        $data = _pack_le4(strlen($input_data));
        $data .= $input_data;

        $data .= _pack_le4(count($repmeta['entityFrameContainers']));
        foreach ($repmeta['entityFrameContainers'] as $entityFrameContainer) {
            $data .= _pack_le4($entityFrameContainer['unk1']);
            $data .= _pack_le4($entityFrameContainer['unk2']);
            $data .= _pack_le4(count($entityFrameContainer['entityFrames']));
            foreach ($entityFrameContainer['entityFrames'] as $entityFrame) {
                $data .= _pack_le4($entityFrame['time']);
                $data .= _pack_le4($entityFrame['xpos']);
                $data .= _pack_le4($entityFrame['ypos']);
                $data .= _pack_le4($entityFrame['xspeed']);
                $data .= _pack_le4($entityFrame['yspeed']);
            }
        }

        $replay = "DF_RPL1";
        $replay .= _pack_le2($repmeta['header']['unk']);
        $replay .= _pack_le4(strlen($data));
        $replay .= _pack_le4($repmeta['header']['frames']);
        $replay .= _pack_byte($repmeta['header']['character']);
        $replay .= _pack_byte(strlen($repmeta['header']['level']));
        $replay .= $repmeta['header']['level'];
        $replay .= gzcompress($data);

        return $replay;
    }

    function _get_pos_edge($input, $from, $to) {
        $result = array();
        for ($i = 0; $i + 1 < strlen($input); $i++) {
            if ($input[$i] == $from && $input[$i + 1] == $to) {
                $result[] = $i;
            }
        }
        return $result;
    }

    function get_jumps($repmeta) {
        return _get_pos_edge($repmeta['inputs'][2], '0', '1');
    }

    function get_dashes($repmeta) {
        return _get_pos_edge($repmeta['inputs'][3], '0', '1');
    }

    function get_downdashes($repmeta) {
        return _get_pos_edge($repmeta['inputs'][4], '0', '1');
    }

    function get_lights($repmeta) {
        return _get_pos_edge($repmeta['inputs'][5], '0', 'a');
    }

    function get_heavies($repmeta) {
        return _get_pos_edge($repmeta['inputs'][6], '0', 'a');
    }
?>

<!--
<msg555> POST to dustkid.com/backend8/add_score.php with post parameters 'character', 'level', 'time', 'user', 'score1', 'score2', 'v0', 'v1', and 'replay'
<msg555> score1 = completion, score2 = finesse, leave v0/v1 empty, and 'replay' is the replay file starting at the DF_RPL1 magic header
<msg555> set user to 0, also
<EklipZ> that build_replay function takes the same array structure that parse_replay returns?
<mfg555> yeah
<EklipZ> replay file starting at the "DF_RPL1" header?
<mfg555> what build_replay returns
<EklipZ> ah kk
<EklipZ> do you know if things break in dustforce if you watch a replay with no sync data?
<mfg555> there's an outer header that hitbox puts around the replay
<mfg555> that includes the username
-->