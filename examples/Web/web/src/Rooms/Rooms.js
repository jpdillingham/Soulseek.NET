import React, { Component, createRef } from 'react';
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

  joinRoom = (roomName) => {
    console.log('join', roomName);
  }

  render = () => {
    const { rooms, active } = this.state;

    return (
      <div className='rooms-container'>
        <Segment className='rooms-menu' raised>
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