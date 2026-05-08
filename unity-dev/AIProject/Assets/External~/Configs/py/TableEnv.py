from ConfigSide import ConfigSide


class TableEnv:
    def __init__(self, side: ConfigSide, err_file):
        self.side = side
        self.err_file = err_file
