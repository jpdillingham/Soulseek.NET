import React, { useMemo } from 'react';
import './Rooms.css';

import {
  Icon, Flag, List, Popup
} from 'semantic-ui-react';

const RoomUserList = ({ users }) => {
  const getFlag = (user) => {
    if (!(user || {}).countryCode) return <Icon className='unknown-user-flag' name='question'/>;

    return <Flag name={user.countryCode.toLowerCase()}/>;
  }

  const getDetails = (user) => {
    return JSON.stringify(user);
  }

  const sortedUsers = useMemo(() => {
    const filtered = [...users]
      .sort((a, b) => a.username.localeCompare(b.username))
      .reduce((acc, user) => {
        (user.status === 'Online' ? acc.online : acc.offline).push(user);
        return acc;
      }, { online: [], offline: []});

    return [...filtered.online, ...filtered.offline];
  }, [users]);

  return (
    <List>
      {sortedUsers.map((user, index) => 
        <List.Item key={index}>
          <List.Content style={{ opacity: user.status === 'Online' ? 1 : .5}}>
            <Popup content={getDetails(user)} trigger={getFlag(user)}/>
            {user.username}
          </List.Content>
        </List.Item>
      )}
    </List>
  )
}

export default RoomUserList;