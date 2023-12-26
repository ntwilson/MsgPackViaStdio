
import sys
from dataclasses import dataclass
import msgpack

@dataclass(frozen=True)
class RoundTripData:
    inputMatrix: list[list[float]]
    weights: list[float]

    @staticmethod
    def fromMsgPack(bytes):
        (inputMatrix, weights) = msgpack.unpackb(bytes, raw=True, strict_map_key=True, use_list=False)
        return RoundTripData(inputMatrix, weights)

    def toMsgPack(self):
        asTuple = (self.inputMatrix, self.weights)
        return msgpack.packb(asTuple, use_bin_type=True)


bytes = sys.stdin.buffer.read()
inputs = RoundTripData.fromMsgPack(bytes)

sys.stdout.buffer.write(inputs.toMsgPack())

