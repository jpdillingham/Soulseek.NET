import React, { Component, createRef } from 'react';
import api from '../api';
import { activeRoomKey } from '../config';

import { Segment } from 'semantic-ui-react';

import RoomMenu from './RoomMenu';

const initialState = {
  active: '',
  rooms: {},
  intervals: {
    messages: undefined,
    users: undefined
  }
}

class Rooms extends Component {
  state = initialState;
  messageRef = undefined;
  listRef = createRef();

  selectRoom = (roomName) => {
    this.setState({ active: roomName }, () => sessionStorage.setItem(activeRoomKey, roomName));
  }

  joinRoom = async (roomName) => {
    await api.post(`/rooms/joined/${roomName}`);
  }

  leaveRoom = async (roomName) => {
    await api.delete(`/rooms/joined/${roomName}`);
  }

  render = () => {
    const { rooms, active } = this.state;

    return (
      <div className='rooms'>
        <Segment raised>
          <RoomMenu
            rooms={rooms}
            active={active}
            onRoomChange={(name) => this.selectRoom(name)}
            joinRoom={this.joinRoom}
          />
        </Segment>
      </div>
    )
  }
}

export default Rooms;